namespace x360NANDManager.MMC {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;

    internal sealed class MMCFlasher : FlasherOutput, IMMCFlasher {
        private readonly MMCDevice _device;
        private readonly int _sectorSize;
        private bool _abort;
        private int _bufsize;
        private Stopwatch _sw;

        public MMCFlasher(MMCDevice device) {
            _device = device;
            _sectorSize = (int) _device.DiskGeometryEX.Geometry.BytesPerSector;
        }

        private void SetBufSize() { _bufsize = (int) ((_device.DiskGeometryEX.Geometry.BytesPerSector * _device.DiskGeometryEX.Geometry.SectorsPerTrack) + _device.DiskGeometryEX.Geometry.BytesPerSector); }

        private void SetBufSize(long sector, long lastsector) {
            if (sector + (_bufsize / _sectorSize) > lastsector)
                _bufsize = (int) ((lastsector - sector) * _sectorSize);
            else
                SetBufSize();
        }

        private void SetBufSizeEX(long offset, long end) {
            if (offset + _bufsize > end)
                _bufsize = (int) (end - offset);
            else
                SetBufSize();
        }

        private void CheckDeviceState() {
            if (_device == null)
                throw new NullReferenceException("_device");
            _abort = false;
        }

        private void CheckSizeArgs(long startSector, ref long sectorCount, long filelen = 0) {
            var sectorSize = _device.DiskGeometryEX.Geometry.BytesPerSector;
            if (sectorCount == 0)
                sectorCount = _device.Size / sectorSize;
            if (filelen != 0) {
                if (filelen > _device.Size)
                    throw new ArgumentOutOfRangeException("filelen");
                if (sectorCount * sectorSize > filelen) {
                    sectorCount = filelen / sectorSize;
                    if (filelen % sectorSize != 0)
                        sectorCount++;
                }
            }
            if (_device.Size >= (startSector + sectorCount) * sectorSize && startSector >= 0 && sectorCount > 0)
                return;
            if ((startSector * sectorSize) > _device.Size || startSector < 0)
                throw new ArgumentOutOfRangeException("startSector");
            if ((sectorCount * sectorSize) > _device.Size || sectorCount < 0)
                throw new ArgumentOutOfRangeException("sectorCount");
            throw new Exception("Too many Sectors specified!");
        }

        private void CheckSizeArgsEX(long offset, ref long length, long filelen = 0) {
            if (length == 0)
                length = _device.Size;
            if (filelen != 0) {
                if (filelen > _device.Size)
                    throw new ArgumentOutOfRangeException("filelen");
                if (length > filelen)
                    length = filelen;
            }
            if (_device.Size >= offset + length && offset >= 0 && length > 0)
                return;
            if (offset > _device.Size || offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (length > _device.Size || length < 0)
                throw new ArgumentOutOfRangeException("length");
            throw new Exception("Offset + Length is bigger then the device!");
        }

        #region Implementation of IMMCFlasher

        public void Release() {
            if (_device == null || !_device.IsLocked)
                return;
            if (_device != null)
                _device.Release();
        }

        public void Abort() { _abort = true; }

        public void ZeroData(long startSector, long sectorCount) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount);
            _device.OpenHandle();
            try {
                var lastsector = startSector + sectorCount;
                UpdateStatus(string.Format("Zeroing data on MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                SetBufSize(startSector, lastsector);
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                var data = new byte[_bufsize];
                for (var sector = startSector; sector < lastsector;) {
                    if (_abort)
                        return;
                    SetBufSize(sector, lastsector);
                    UpdateMMCProgress(sector, lastsector, _sectorSize, _bufsize);
                    if (sector + (_bufsize / _sectorSize) > lastsector)
                        Array.Resize(ref data, (int) ((lastsector - sector) * _sectorSize));
                    _device.WriteToDevice(ref data, sector * _sectorSize);
                    sector += _bufsize / _sectorSize;
                }
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if (_device != null)
                    _device.Release();
            }
        }

        public void ZeroDataEX(long offset = 0, long length = 0) {
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length);
            _device.OpenHandle();
            var end = offset + length;
            try {
                UpdateStatus(string.Format("Zeroing data on MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Size: 0x{2:X}", _bufsize, _sectorSize, end));
                for (var current = offset; current < end;) {
                    if (_abort)
                        return;
                    SetBufSizeEX(current, end);
                    UpdateMMCProgressEX(current, end, _bufsize);
                    var data = new byte[_bufsize];
                    _device.WriteToDevice(ref data, current);
                    current += data.Length;
                }
            }
            finally {
                if (_device != null)
                    _device.Release();
            }
        }

        public void Write(byte[] data, long startSector = 0, long sectorCount = 0, bool verify = true) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount, data.Length);
            _device.OpenHandle();
            long doffset = 0;
            try {
                var lastsector = startSector + sectorCount;
                var maxSector = lastsector;
                if (verify)
                    maxSector += lastsector;
                UpdateStatus(string.Format("Writing data to MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                SetBufSize(startSector, lastsector);
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                for (var sector = startSector; sector < lastsector;) {
                    if (_abort)
                        return;
                    SetBufSize(sector, lastsector);
                    UpdateMMCProgress(sector, maxSector, _sectorSize, _bufsize);
                    _device.WriteToDevice(ref data, sector * _sectorSize, (int) doffset, _bufsize);
                    doffset += _bufsize;
                    sector += _bufsize / _sectorSize;
                }

                #region Verification

                if (!verify)
                    return;
                doffset = 0;
                _device.Release();
                _device.OpenHandle();
                UpdateStatus(string.Format("Verifying data on MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                for (var sector = startSector; sector < lastsector;) {
                    if (_abort)
                        return;
                    SetBufSize(sector, lastsector);
                    UpdateMMCProgress(sector + lastsector, maxSector, _sectorSize, _bufsize);
                    var buf = new byte[_bufsize];
                    var read = _device.ReadFromDevice(ref buf, sector * _sectorSize);
                    if (read != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    if (!CompareByteArrays(ref buf, ref data, (int) doffset))
                        SendError(string.Format("Verification failed somewhere between Sector: 0x{0:X} and 0x{1:X}", sector - (_bufsize / _sectorSize), sector));
                    doffset += read;
                    sector += read / _sectorSize;
                }

                #endregion Verification
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if (_device != null)
                    _device.Release();
            }
        }

        public void Write(string file, long startSector = 0, long sectorCount = 0, bool verify = true) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount);
            _device.OpenHandle();
            using (var br = OpenReader(file)) {
                try {
                    var lastsector = startSector + sectorCount;
                    var maxSector = lastsector;
                    if (verify)
                        maxSector += lastsector;
                    UpdateStatus(string.Format("Writing data to MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                    SetBufSize(startSector, lastsector);
                    Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                    for (var sector = startSector; sector < lastsector;) {
                        if (_abort)
                            return;
                        SetBufSize(sector, lastsector);
                        UpdateMMCProgress(sector, maxSector, _sectorSize, _bufsize);
                        var data = br.ReadBytes(_bufsize);
                        _device.WriteToDevice(ref data, sector * _sectorSize);
                        sector += _bufsize / _sectorSize;
                    }

                    #region Verification

                    if (!verify)
                        return;
                    br.BaseStream.Seek(0, SeekOrigin.Begin);
                    UpdateStatus(string.Format("Verifying data on MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                    for (var sector = startSector; sector < lastsector;) {
                        if (_abort)
                            return;
                        SetBufSize(sector, lastsector);
                        UpdateMMCProgress(sector + lastsector, maxSector, _sectorSize, _bufsize);
                        var buf = new byte[_bufsize];
                        var read = _device.ReadFromDevice(ref buf, sector * _sectorSize);
                        if (read != _bufsize)
                            throw new Exception("Something went wrong with the read operation!");
                        var tmp = br.ReadBytes(_bufsize);
                        if (!CompareByteArrays(ref buf, ref tmp))
                            SendError(string.Format("Verification failed somewhere between Sector: 0x{0:X} and 0x{1:X}", sector - (_bufsize / _sectorSize), sector));
                        sector += read / _sectorSize;
                    }

                    #endregion Verification
                }
                finally {
                    _sw.Stop();
                    UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                    if (_device != null)
                        _device.Release();
                }
            }
        }

        public void WriteEX(byte[] data, long offset = 0, long length = 0, bool verify = true) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length, data.Length);
            _device.OpenHandle();
            var doffset = 0;
            try {
                var end = offset + length;
                var maxLen = end;
                if (verify)
                    maxLen += maxLen;
                UpdateStatus(string.Format("Writing data to MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Write Size: 0x{2:X}", _bufsize, _sectorSize, length));
                for (var current = offset; current < end;) {
                    if (_abort)
                        return;
                    SetBufSize(current, end);
                    UpdateMMCProgressEX(current, maxLen, _bufsize);
                    _device.WriteToDevice(ref data, current, doffset, _bufsize);
                    doffset += _bufsize;
                    current += _bufsize;
                }

                #region Verification

                if (!verify)
                    return;
                doffset = 0;
                UpdateStatus(string.Format("Verifying data on MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                for (var current = offset; current < end;) {
                    if (_abort)
                        return;
                    SetBufSize(current, end);
                    UpdateMMCProgressEX(current + end, maxLen, _bufsize);
                    var buf = new byte[_bufsize];
                    if (_device.ReadFromDevice(ref buf, current) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    if (!CompareByteArrays(ref buf, ref data, doffset))
                        SendError(string.Format("Verification failed somewhere between Offset: 0x{0:X} and 0x{1:X}", current - _bufsize, current));
                    doffset += _bufsize;
                    current += _bufsize;
                }

                #endregion Verification
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if (_device != null)
                    _device.Release();
            }
        }

        public void WriteEX(string file, long offset = 0, long length = 0, bool verify = true) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length, new FileInfo(file).Length);
            _device.OpenHandle();
            using (var br = OpenReader(file)) {
                try {
                    var end = offset + length;
                    var maxLen = end;
                    if (verify)
                        maxLen += maxLen;
                    UpdateStatus(string.Format("Writing data to MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                    UpdateStatus(string.Format("Writing data from: {0}", file));
                    Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, length));
                    for (var current = offset; current < end;) {
                        if (_abort)
                            return;
                        SetBufSize(current, end);
                        UpdateMMCProgressEX(current, maxLen, _bufsize);
                        var data = br.ReadBytes(_bufsize);
                        _device.WriteToDevice(ref data, current);
                        current += data.Length;
                    }

                    #region Verification

                    if (!verify)
                        return;
                    br.BaseStream.Seek(0, SeekOrigin.Begin);
                    UpdateStatus(string.Format("Verifying data on MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                    for (var current = offset; current < end;) {
                        if (_abort)
                            return;
                        SetBufSize(current, end);
                        UpdateMMCProgressEX(current + end, maxLen, _bufsize);
                        var buf = new byte[_bufsize];
                        if (_device.ReadFromDevice(ref buf, current) != _bufsize)
                            throw new Exception("Something went wrong with the read operation!");
                        var tmp = br.ReadBytes(_bufsize);
                        if (!CompareByteArrays(ref buf, ref tmp))
                            SendError(string.Format("Verification failed somewhere between Offset: 0x{0:X} and 0x{1:X}", current - _bufsize, current));
                        current += _bufsize;
                    }

                    #endregion Verification
                }
                finally {
                    _sw.Stop();
                    UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                    if (_device != null)
                        _device.Release();
                }
            }
        }

        public byte[] Read(long startSector = 0, long sectorCount = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount);
            _device.OpenHandle();
            var data = new List <byte>();
            try {
                var lastsector = startSector + sectorCount;
                UpdateStatus(string.Format("Reading data from MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                for (var sector = startSector; sector < lastsector;) {
                    if (_abort)
                        return data.ToArray();
                    SetBufSize(sector, lastsector);
                    UpdateMMCProgress(sector, lastsector, _sectorSize, _bufsize);
                    var buf = new byte[_bufsize];
                    var read = _device.ReadFromDevice(ref buf, sector * _sectorSize);
                    if (read != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    data.AddRange(buf);
                    sector += read / _sectorSize;
                }
                return data.ToArray();
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if (_device != null)
                    _device.Release();
            }
        }

        public void Read(string file, long startSector = 0, long sectorCount = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgs(startSector, ref sectorCount);
            _device.OpenHandle();
            if (GetTotalFreeSpace(file) < sectorCount * _sectorSize)
                throw new Exception("Not enough space for the dump!");
            using (var bw = OpenWriter(file)) {
                try {
                    var lastsector = startSector + sectorCount;
                    UpdateStatus(string.Format("Reading data from MMC Sectors: 0x{0:X} to 0x{1:X}", startSector, lastsector));
                    UpdateStatus(string.Format("Saving data to: {0}", file));
                    SetBufSize(startSector, lastsector);
                    Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, sectorCount * _sectorSize));
                    for (var sector = startSector; sector < lastsector;) {
                        if (_abort)
                            return;
                        SetBufSize(sector, lastsector);
                        UpdateMMCProgress(sector, lastsector, _sectorSize, _bufsize);
                        var buf = new byte[_bufsize];
                        var read = _device.ReadFromDevice(ref buf, sector * _sectorSize);
                        if (read != _bufsize)
                            throw new Exception("Something went wrong with the read operation!");
                        bw.Write(buf);
                        sector += read / _sectorSize;
                    }
                }
                finally {
                    _sw.Stop();
                    UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                    if (_device != null)
                        _device.Release();
                }
            }
        }

        public void Read(IEnumerable <string> files, long startSector = 0, long sectorCount = 0) {
            _abort = false;
            var sw = Stopwatch.StartNew();
            files = RemoveDuplicatesInList(files);
            foreach (var file in files) {
                if (_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Read aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                Read(file, startSector, sectorCount);
            }
            if (_abort)
                return;
            sw.Stop();
            UpdateStatus(string.Format("Read completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
        }

        public byte[] ReadEX(long offset = 0, long length = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length);
            _device.OpenHandle();
            var data = new List <byte>();
            try {
                var end = offset + length;
                UpdateStatus(string.Format("Reading data from MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, length));
                for (var current = offset; current < end;) {
                    if (_abort)
                        return data.ToArray();
                    SetBufSizeEX(current, end);
                    UpdateMMCProgressEX(current, end, _bufsize);
                    var buf = new byte[_bufsize];
                    if (_device.ReadFromDevice(ref buf, current) != _bufsize)
                        throw new Exception("Something went wrong with the read operation!");
                    data.AddRange(buf);
                    current += buf.Length;
                }
                return data.ToArray();
            }
            finally {
                _sw.Stop();
                UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                if (_device != null)
                    _device.Release();
            }
        }

        public void ReadEX(string file, long offset = 0, long length = 0) {
            _sw = Stopwatch.StartNew();
            CheckDeviceState();
            CheckSizeArgsEX(offset, ref length);
            _device.OpenHandle();
            if (GetTotalFreeSpace(file) < length)
                throw new Exception("Not enough space for the dump!");
            using (var bw = OpenWriter(file)) {
                try {
                    var end = offset + length;
                    UpdateStatus(string.Format("Reading data from MMC Offset: 0x{0:X} to 0x{1:X}", offset, end));
                    Main.SendDebug(string.Format("Bufsize: 0x{0:X} Sector Size: 0x{1:X} Total Dump Size: 0x{2:X}", _bufsize, _sectorSize, length));
                    for (var current = offset; current < end;) {
                        if (_abort)
                            return;
                        SetBufSizeEX(current, end);
                        UpdateMMCProgressEX(current, end, _bufsize);
                        var buf = new byte[_bufsize];
                        if (_device.ReadFromDevice(ref buf, current) != _bufsize)
                            throw new Exception("Something went wrong with the read operation!");
                        bw.Write(buf);
                        current += buf.Length;
                    }
                }
                finally {
                    _sw.Stop();
                    UpdateStatus(string.Format((_abort ? "Aborted after: {0:F0} Minutes {1:F0} Seconds" : "Completed after: {0:F0} Minutes {1:F0} Seconds"), _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
                    if (_device != null)
                        _device.Release();
                }
            }
        }

        public void ReadEX(IEnumerable <string> files, long offset = 0, long length = 0) {
            _abort = false;
            var sw = Stopwatch.StartNew();
            files = RemoveDuplicatesInList(files);
            foreach (var file in files) {
                if (_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Read aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                ReadEX(file, offset, length);
            }
            if (_abort)
                return;
            sw.Stop();
            UpdateStatus(string.Format("Read completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
        }

        /// <summary>
        ///     Gets a list of Devices that can be selected
        /// </summary>
        /// <param name="onlyRemoveable"> If set to false also Fixed devices will be included in the list (most likely useless) </param>
        /// <returns> List of devices used for the MMC Flasher </returns>
        internal static IList <MMCDevice> GetDevices(bool onlyRemoveable = true) {
            var tmp = new Dictionary <int, MMCDevice>();
            foreach (var drive in DriveInfo.GetDrives()) {
                if (drive.DriveType == DriveType.Fixed && onlyRemoveable)
                    continue;
                if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Fixed)
                    continue;
                try {
                    var devnum = NativeWin32.GetDeviceNumber(drive.Name);
                    if (!tmp.ContainsKey(devnum)) {
                        var path = NativeWin32.GetDevicePath(drive.Name);
                        tmp.Add(devnum, new MMCDevice(drive.Name, path, NativeWin32.GetGeometryEX(path)));
                    }
                    else {
                        tmp[devnum].DisplayName = string.Format("{0}, {1}", tmp[devnum].DisplayName, drive.Name);
                        tmp[devnum].AddVolume(drive.Name);
                    }
                }
                catch (Exception ex) {
                    var dex = ex as X360NANDManagerException;
                    if (dex != null && (dex.Win32ErrorNumber == 32 || dex.Win32ErrorNumber == 0 /* Success, not an error?! */|| dex.Win32ErrorNumber == 21 /* Device not ready... ignore it... */))
                        continue;
                    var wex = ex as Win32Exception;
                    if (wex != null && wex.NativeErrorCode == 21) //Device not ready, Win32 Error outside of my own error handling...
                        continue;
                    throw;
                }
            }
            Main.SendDebug("Copying data to returnable object");
            var ret = new MMCDevice[tmp.Values.Count];
            tmp.Values.CopyTo(ret, 0);
            return ret;
        }

        #endregion
    }
}