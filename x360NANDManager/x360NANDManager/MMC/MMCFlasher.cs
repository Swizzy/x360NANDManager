namespace x360NANDManager.MMC {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    internal sealed class MMCFlasher : FlasherOutput, IMMCFlasher {
        private readonly MMCDevice _device;
        private readonly int _sectorSize;
        private bool _abort;
        private long _bufsize;
        private Stopwatch _sw;

        public MMCFlasher(MMCDevice device) {
            _device = device;
            _sectorSize = (int) _device.DiskGeometryEX.Geometry.BytesPerSector;
        }

        private void SetBufSize() {
            _bufsize = (_device.DiskGeometryEX.Geometry.BytesPerSector * _device.DiskGeometryEX.Geometry.SectorsPerTrack) + _device.DiskGeometryEX.Geometry.BytesPerSector;
        }

        private void SetBufSize(long sector, long lastsector) {
            if(sector + (_bufsize / _sectorSize) > lastsector)
                _bufsize = (lastsector - sector) * _sectorSize;
            else
                SetBufSize();
        }

        private void SetBufSizeEX(long offset, long end) {
            if(offset + _bufsize > end)
                _bufsize = end - offset;
            else
                SetBufSize();
        }

        private static void SeekFStream(ref FileStream stream, long offset, SeekOrigin origin = SeekOrigin.Begin) {
            if(stream.Position == offset)
                return;
            if(stream.CanSeek)
                stream.Seek(offset, origin);
            else
                throw new Exception("Unable to seek!");
        }

        private void CheckDeviceState() {
            if(_device == null)
                throw new NullReferenceException("_device");
            _abort = false;
        }

        private void CheckSizeArgs(long startSector, ref long sectorCount, long filelen = 0) {
            var sectorSize = _device.DiskGeometryEX.Geometry.BytesPerSector;
            if(sectorCount == 0)
                sectorCount = _device.Size / sectorSize;
            if(filelen != 0) {
                if(filelen > _device.Size)
                    throw new ArgumentOutOfRangeException("filelen");
                if(sectorCount * sectorSize > filelen) {
                    sectorCount = filelen / sectorSize;
                    if(filelen % sectorSize != 0)
                        sectorCount++;
                }
            }
            if(_device.Size >= (startSector + sectorCount) * sectorSize && startSector >= 0 && sectorCount > 0)
                return;
            if((startSector * sectorSize) > _device.Size || startSector < 0)
                throw new ArgumentOutOfRangeException("startSector");
            if((sectorCount * sectorSize) > _device.Size || sectorCount < 0)
                throw new ArgumentOutOfRangeException("sectorCount");
            throw new Exception("Too many Sectors specified!");
        }

        private void CheckSizeArgsEX(long offset, ref long length, long filelen = 0) {
            if(length == 0)
                length = _device.Size;
            if(filelen != 0) {
                if(filelen > _device.Size)
                    throw new ArgumentOutOfRangeException("filelen");
                if(length > filelen)
                    length = filelen;
            }
            if(_device.Size >= offset + length && offset >= 0 && length > 0)
                return;
            if(offset > _device.Size || offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if(length > _device.Size || length < 0)
                throw new ArgumentOutOfRangeException("length");
            throw new Exception("Offset + Length is bigger then the device!");
        }

        #region Implementation of IMMCFlasher

        public void Release() {
            if(_device != null && _device.IsLocked) {
                if(_device != null)
                    _device.Release();
            }
        }

        public void Reset(bool read = true) {
            if(read)
                _device.OpenReadHandle();
            else
                _device.OpenWriteHandle();
        }

        public void Abort() {
            _abort = true;
        }

        public void ZeroData(long startSector, long sectorCount) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount);
            _device.OpenWriteHandle();
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Read);
            try {
                SeekFStream(ref stream, startSector * _sectorSize);
                var lastsector = startSector + sectorCount;
                UpdateStatus(string.Format("Zeroing data on MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                for(; stream.Position / _sectorSize < lastsector;) {
                    if(_abort)
                        return;
                    SetBufSize(stream.Position / _sectorSize, lastsector);
                    UpdateMMCProgress(stream.Position / _sectorSize, lastsector, _sectorSize, _bufsize);
                    var buf = new byte[_bufsize];
                    stream.Write(buf, 0, buf.Length);
                }
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void ZeroDataEX(long offset = 0, long length = 0) {
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length);
            _device.OpenWriteHandle();
            var end = offset + length;
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Write);
            try {
                SeekFStream(ref stream, offset);
                UpdateStatus(string.Format("Zeroing data on MMC Offset: 0x{0:X} to 0x{1:X}", offset, length));
                for(; stream.Position < end;) {
                    SetBufSizeEX(stream.Position, end);
                    UpdateMMCProgressEX(stream.Position, end, _bufsize);
                    var data = new byte[_bufsize];
                    stream.Write(data, 0, data.Length);
                }
            }
            finally {
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void Write(byte[] data, long startSector = 0, long sectorCount = 0, bool verify = true) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount, data.Length);
            _device.OpenWriteHandle();
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Write);
            long doffset = 0;
            try {
                SeekFStream(ref stream, startSector * _sectorSize);
                var lastsector = startSector + sectorCount;
                var maxSector = lastsector;
                if(verify)
                    maxSector += lastsector;
                UpdateStatus(string.Format("Writing data to MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                for(; stream.Position / _sectorSize < lastsector;) {
                    if(_abort)
                        return;
                    SetBufSize(stream.Position / _sectorSize, lastsector);
                    UpdateMMCProgress(stream.Position / _sectorSize, maxSector, _sectorSize, _bufsize);
                    stream.Write(data, (int) doffset, (int) _bufsize);
                    doffset += _bufsize;
                }
                if(!verify)
                    return;
                doffset = 0;
                stream.Close();
                _device.Release();
                _device.OpenReadHandle();
                stream = new FileStream(_device.DeviceHandle, FileAccess.Read);
                UpdateStatus(string.Format("Verifying data on MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                for(; stream.Position / _sectorSize < lastsector;) {
                    if(_abort)
                        return;
                    SetBufSize(stream.Position / _sectorSize, lastsector);
                    UpdateMMCProgress((stream.Position / _sectorSize) + lastsector, maxSector, _sectorSize, _bufsize);
                    var buf = new byte[_bufsize];
                    if(stream.Read(buf, 0, buf.Length) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    if(!CompareByteArrays(ref buf, ref data, (int) doffset))
                        SendError(string.Format("Verification failed somewhere between Sector: 0x{0:X} and 0x{1:X}", (stream.Position - _bufsize) / _sectorSize, stream.Position / _sectorSize));
                    doffset += _bufsize;
                }
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void Write(string file, long startSector = 0, long sectorCount = 0, bool verify = true) {
            throw new NotImplementedException();
        }

        public void WriteEX(byte[] data, long offset = 0, long length = 0, bool verify = true) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length, data.Length);
            _device.OpenWriteHandle();
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Write);
            long doffset = 0;
            try {
                SeekFStream(ref stream, offset);
                var end = offset + length;
                var maxLen = end;
                if(verify)
                    maxLen += maxLen;
                UpdateStatus(string.Format("Writing data to MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Write Size: 0x{2:X}", _bufsize, _sectorSize, length));
                for(; stream.Position < end;) {
                    if(_abort)
                        return;
                    SetBufSize(stream.Position, end);
                    UpdateMMCProgressEX(stream.Position, maxLen, _bufsize);
                    stream.Write(data, (int) doffset, (int) _bufsize);
                    doffset += _bufsize;
                }
                if(!verify)
                    return;
                doffset = 0;
                stream.Close();
                _device.Release();
                _device.OpenReadHandle();
                stream = new FileStream(_device.DeviceHandle, FileAccess.Read);
                SeekFStream(ref stream, offset);
                UpdateStatus(string.Format("Verifying data on MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                for(; stream.Position < end;) {
                    if(_abort)
                        return;
                    SetBufSize(stream.Position, end);
                    UpdateMMCProgressEX(stream.Position + end, maxLen, _bufsize);
                    var buf = new byte[_bufsize];
                    if(stream.Read(buf, 0, buf.Length) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    if(!CompareByteArrays(ref buf, ref data, (int) doffset))
                        SendError(string.Format("Verification failed somewhere between Offset: 0x{0:X} and 0x{1:X}", stream.Position - _bufsize, stream.Position));
                    doffset += _bufsize;
                }
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void WriteEX(string file, long offset = 0, long length = 0, bool verify = true) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length, new FileInfo(file).Length);
            _device.OpenWriteHandle();
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Write);
            var br = OpenReader(file);
            try {
                SeekFStream(ref stream, offset);
                var end = offset + length;
                var maxLen = end;
                if(verify)
                    maxLen += maxLen;
                UpdateStatus(string.Format("Writing data to MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                UpdateStatus(string.Format("Writing data from: {0}", file));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, length));
                for(; stream.Position < end;) {
                    if(_abort)
                        return;
                    SetBufSize(stream.Position, end);
                    UpdateMMCProgressEX(stream.Position, maxLen, _bufsize);
                    var data = br.ReadBytes((int) _bufsize);
                    stream.Write(data, 0, data.Length);
                }
                //TODO: Verify
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if(br != null)
                    br.Close();
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public byte[] Read(long startSector = 0, long sectorCount = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount);
            _device.OpenReadHandle();
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Read);
            var data = new List<byte>();
            try {
                SeekFStream(ref stream, startSector * _sectorSize);
                var lastsector = startSector + sectorCount;
                UpdateStatus(string.Format("Reading data from MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                for(; stream.Position / _sectorSize < lastsector;) {
                    if(_abort)
                        return data.ToArray();
                    SetBufSize(stream.Position / _sectorSize, lastsector);
                    UpdateMMCProgress(stream.Position / _sectorSize, lastsector, _sectorSize, _bufsize);
                    var buf = new byte[_bufsize];
                    if(stream.Read(buf, 0, buf.Length) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    data.AddRange(buf);
                }
                return data.ToArray();
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void Read(string file, long startSector = 0, long sectorCount = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount);
            _device.OpenReadHandle();
            if(GetTotalFreeSpace(file) < sectorCount * _sectorSize)
                throw new Exception("Not enough space for the dump!");
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Read);
            var bw = OpenWriter(file);
            try {
                SeekFStream(ref stream, startSector * _sectorSize);
                var lastsector = startSector + sectorCount;
                UpdateStatus(string.Format("Reading data from MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                UpdateStatus(string.Format("Saving data to: {0}", file));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                for(; stream.Position / _sectorSize < lastsector;) {
                    if(_abort)
                        return;
                    SetBufSize(stream.Position / _sectorSize, lastsector);
                    UpdateMMCProgress(stream.Position / _sectorSize, lastsector, _sectorSize, _bufsize);
                    var buf = new byte[_bufsize];
                    if(stream.Read(buf, 0, buf.Length) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    bw.Write(buf, 0, buf.Length);
                }
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if(bw != null)
                    bw.Close();
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void Read(IEnumerable<string> files, long startSector = 0, long sectorCount = 0) {
            _abort = false;
            var sw = Stopwatch.StartNew();
            files = RemoveDuplicatesInList(files);
            foreach(var file in files) {
                if(_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Read aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                Read(file, startSector, sectorCount);
                Reset();
            }
            if(_abort)
                return;
            sw.Stop();
            UpdateStatus(string.Format("Read completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
        }

        public byte[] ReadEX(long offset = 0, long length = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length);
            _device.OpenReadHandle();
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Read);
            var data = new List<byte>();
            try {
                SeekFStream(ref stream, offset);
                var end = offset + length;
                UpdateStatus(string.Format("Reading data from MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, length));
                for(; stream.Position < end;) {
                    if(_abort)
                        return data.ToArray();
                    SetBufSizeEX(stream.Position, end);
                    UpdateMMCProgressEX(stream.Position, end, _bufsize);
                    var buf = new byte[_bufsize];
                    if(stream.Read(buf, 0, buf.Length) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    data.AddRange(buf);
                }
                return data.ToArray();
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void ReadEX(string file, long offset = 0, long length = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length);
            _device.OpenReadHandle();
            if(GetTotalFreeSpace(file) < length)
                throw new Exception("Not enough space for the dump!");
            var stream = new FileStream(_device.DeviceHandle, FileAccess.Read);
            var bw = OpenWriter(file);
            try {
                SeekFStream(ref stream, offset);
                var end = offset + length;
                UpdateStatus(string.Format("Reading data from MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, length));
                for(; stream.Position < end;) {
                    if(_abort)
                        return;
                    SetBufSizeEX(stream.Position, end);
                    UpdateMMCProgressEX(stream.Position, end, _bufsize);
                    var buf = new byte[_bufsize];
                    if(stream.Read(buf, 0, buf.Length) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    bw.Write(buf, 0, buf.Length);
                }
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if(bw != null)
                    bw.Close();
                if(stream != null)
                    stream.Close();
                if(_device != null)
                    _device.Release();
            }
        }

        public void ReadEX(IEnumerable<string> files, long offset = 0, long length = 0) {
            _abort = false;
            var sw = Stopwatch.StartNew();
            files = RemoveDuplicatesInList(files);
            foreach(var file in files) {
                if(_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Read aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                ReadEX(file, offset, length);
                Reset();
            }
            if(_abort)
                return;
            sw.Stop();
            UpdateStatus(string.Format("Read completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
        }

        /// <summary>
        ///   Gets a list of Devices that can be selected
        /// </summary>
        /// <param name="onlyRemoveable"> If set to false also Fixed devices will be included in the list (most likely useless) </param>
        /// <returns> List of devices used for the MMC Flasher </returns>
        internal static IList<MMCDevice> GetDevices(bool onlyRemoveable = true) {
            var tmp = new Dictionary<int, MMCDevice>();
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.DriveType == DriveType.Fixed && onlyRemoveable)
                    continue;
                if(drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Fixed)
                    continue;
                try {
                    Main.SendDebug(string.Format("Getting Drive number for Device: {0}", drive.Name));
                    var devnum = NativeWin32.GetDeviceNumber(drive.Name);
                    if(!tmp.ContainsKey(devnum)) {
                        Main.SendDebug(string.Format("Getting Drive path for Device: {0}", drive.Name));
                        var path = NativeWin32.GetDevicePath(drive.Name);
                        Main.SendDebug(string.Format("Getting Drive Geometry for Device: {0}", drive.Name));
                        tmp.Add(devnum, new MMCDevice(drive.Name, path, NativeWin32.GetGeometryEX(path)));
                        GetTotalFreeSpace(drive.Name);
                    }
                    else
                        tmp[devnum].DisplayName = string.Format("{0}, {1}", tmp[devnum].DisplayName, drive.Name);
                }
                catch(Exception ex) {
                    var dex = ex as DeviceError;
                    if(dex != null && dex.Win32ErrorNumber == 32)
                        continue;
                    throw;
                }
            }
            Main.SendDebug("Copying data to returnable object");
            var ret = new MMCDevice[tmp.Values.Count];
            tmp.Values.CopyTo(ret, 0);
            return ret;
        }

        #endregion Implementation of IMMCFlasher
    }
}