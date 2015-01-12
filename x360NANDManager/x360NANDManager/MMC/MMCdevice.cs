namespace x360NANDManager.MMC {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using Microsoft.Win32.SafeHandles;

    public sealed class MMCDevice : Utils, IDisposable {
        private class MountPoint {
            public readonly string MountPath;
            public readonly string VolumePath;

            public MountPoint(string mountPath) {
                MountPath = mountPath;
                VolumePath = NativeWin32.GetVolumeGuidPath(MountPath);
            }
        }

        private SafeFileHandle _deviceHandle;
        private FileStream _fileStream;
        private readonly List<string> _volumes = new List<string>();
        private readonly List<MountPoint> _mountPoints = new List<MountPoint>();

        internal MMCDevice(string displayName, string path, NativeWin32.DiskGeometryEX geometry) {
            _volumes.Add(displayName);
            DisplayName = displayName;
            Path = path;
            DiskGeometryEX = geometry;
        }

        /// <summary>
        ///   Returns true if there is a device lock in place (exclusive access)
        /// </summary>
        internal bool IsLocked { get; private set; }

        /// <summary>
        /// Gets or sets this device to be read/write using nandMMC style (CreateFile -> FileStream [true]) or Pure WINAPI  (CreateFile -> ReadFile/WriteFile [false])
        /// </summary>
        public bool NANDMMCStyle { get; set; }

        /// <summary>
        ///   Gets Disk Size based on DiskGeometry
        /// </summary>
        public long Size {
            get { return (long) DiskGeometryEX.DiskSize; }
        }

        /// <summary>
        ///   Device Path
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///   Name to display to the end-user
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        ///   Gets the disk Geometry for the current device
        /// </summary>
        public NativeWin32.DiskGeometryEX DiskGeometryEX { get; private set; }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            GC.SuppressFinalize(this);
            try {
                Release();
            }
            catch (Exception ex) {
                Main.SendDebug(ex.ToString());
            }
        }

        #endregion

        private void SeekFSStream(long offset)
        {
            if(NANDMMCStyle) {
                if(_fileStream.Position == offset)
                    return; // No need to seek, we're already where we want to be...
                if(_fileStream.CanSeek)
                    _fileStream.Seek(offset, SeekOrigin.Begin);
                else
                    throw new Exception("Unable to seek!");
            }
            else {
                var lo = (int) (offset & 0xffffffff);
                var hi = (int) (offset << 32);
                lo = (int) NativeWin32.SetFilePointer(_deviceHandle, lo, ref hi, NativeWin32.SeekOriginToMoveMethod(SeekOrigin.Begin));
                if(lo == -1)
                    throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.Win32Error);
            }
        }

        ~MMCDevice() {
            try {
                Release();
            }
            catch (Exception ex) {
                Main.SendDebug(ex.ToString());
            }
        }

        /// <summary>
        ///   Returns a string representation of this device (used for displaying in User controls)
        /// </summary>
        /// <returns> String representation of the device </returns>
        public override string ToString() {
            return string.Format("{0} [ {1} ]", DisplayName, GetSizeReadable(Size));
        }

        /// <summary>
        ///   Open a handle for the device (internal handle)
        /// </summary>
        internal void OpenHandle() {
            Main.SendDebug("Opening Device Handle");
            if(_deviceHandle != null && !_deviceHandle.IsInvalid)
                Release();
            if(!NativeWin32.LockDevice(Path))
                throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.DeviceLockFailed);
            IsLocked = true;
            UnmountDevice();
            Main.SendDebug("Device locked... opening the handle now!");
            _deviceHandle = NativeWin32.GetFileHandleRaw(Path, FileAccess.ReadWrite, FileShare.ReadWrite);
            Main.SendDebug("Device opened!");
            _fileStream = new FileStream(_deviceHandle, FileAccess.ReadWrite);
        }

        /// <summary>
        ///   Release the device
        /// </summary>
        internal void Release() {
            if(_deviceHandle != null && !_deviceHandle.IsInvalid)
                _deviceHandle.Close();
            if (_fileStream != null)
                _fileStream.Close();
            try {
                Main.SendDebug("Unlocking device...");
                NativeWin32.UnLockDevice(Path);
                RemountDevice();
            }
            catch(Win32Exception ex) {
                if(ex.NativeErrorCode != 158) // Already unlocked, not really an error...
                    throw;
            }
            IsLocked = false;
        }

        internal void WriteToDevice(ref byte[] buffer, long offset, int index = 0, int count = -1) {
            SeekFSStream(offset);
            if(count <= 0)
                count = buffer.Length;
#if DEBUG
            Main.SendDebug("Writing 0x{0:X} bytes to the device @ offset 0x{1:X} index in the buf: 0x{2:X}", count, offset, index);
#endif
            if(NANDMMCStyle)
                _fileStream.Write(buffer, index, count);
            else {
                uint written;
                bool res;
                if (index != 0)
                {
                    var tmp = new byte[count];
                    Buffer.BlockCopy(buffer, index, tmp, 0, count);
                    res = NativeWin32.WriteFile(_deviceHandle, tmp, (uint)count, out written, IntPtr.Zero);
                }
                else
                    res = NativeWin32.WriteFile(_deviceHandle, buffer, (uint)count, out written, IntPtr.Zero);
                if (written != count && res)
                    throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.WriteFailed);
                if (!res)
                    throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.Win32Error);
            }
        }

        internal int ReadFromDevice(ref byte[] buffer, long offset, int index = 0, int count = -1) {
            SeekFSStream(offset);
            if(count <= 0)
                count = buffer.Length;
#if DEBUG
            Main.SendDebug("Reading 0x{0:X} bytes from the device @ offset 0x{1:X} index in the buf: 0x{2:X}", count, offset, index);
#endif
            if (NANDMMCStyle)
                return _fileStream.Read(buffer, index, count);
            uint readcount;
            bool res;
            if (index != 0)
                res = NativeWin32.ReadFile(_deviceHandle, buffer, (uint)count, out readcount, IntPtr.Zero);
            else
            {
                var tmp = new byte[count];
                res = NativeWin32.ReadFile(_deviceHandle, tmp, (uint)count, out readcount, IntPtr.Zero);
                if (res)
                    Buffer.BlockCopy(tmp, 0, buffer, index, count);
            }
            if (!res)
                throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.Win32Error);
            return (int)readcount;
        }

        public void AddVolume(string volume) { _volumes.Add(volume); }

        public void UnmountDevice() {
            foreach(var volume in _volumes) {
                SafeFileHandle vHandle;
                NativeWin32.UnmountVolume(volume, out vHandle);
                vHandle.Close();
                _mountPoints.Add(new MountPoint(volume));
                Main.SendDebug("Removing mount point: {0}", volume);
                NativeWin32.DeleteVolumeMountPoint(volume);
            }
        }

        public void RemountDevice() {
            foreach(var mp in _mountPoints) {
                Main.SendDebug("Mounting {0} to {1}", mp.VolumePath, mp.MountPath);
                NativeWin32.SetVolumeMountPoint(mp.MountPath, mp.VolumePath);
            }
        }
    }
}