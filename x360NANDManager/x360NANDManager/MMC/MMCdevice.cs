namespace x360NANDManager.MMC {
    using System;
    using System.IO;
    using Microsoft.Win32.SafeHandles;

    public sealed class MMCDevice : Utils, IDisposable {
        internal SafeFileHandle DeviceHandle;

        internal MMCDevice(string displayName, string path, NativeWin32.DiskGeometryEX geometry) {
            DisplayName = displayName;
            Path = path;
            DiskGeometryEX = geometry;
        }

        /// <summary>
        ///   Returns true if there is a device lock in place (exclusive access)
        /// </summary>
        internal bool IsLocked { get; private set; }

        /// <summary>
        ///   Gets Disk Size based on DiskGeometry
        /// </summary>
        public long Size {
            get {
                return (long) DiskGeometryEX.DiskSize;
            }
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
            catch {
            }
        }

        #endregion

        ~MMCDevice() {
            try {
                Release();
            }
            catch {
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
        ///   Open a handle for reading from the device (internal handle)
        /// </summary>
        internal void OpenReadHandle() {
            if(DeviceHandle != null && !DeviceHandle.IsInvalid)
                DeviceHandle.Close();
            if(!NativeWin32.LockDevice(Path))
                throw new DeviceError(DeviceError.ErrorLevels.DeviceLockFailed);
            IsLocked = true;
            DeviceHandle = NativeWin32.GetFileHandleRaw(Path, FileAccess.Read);
        }

        /// <summary>
        ///   Open a handle for writing to the device (internal handle)
        /// </summary>
        internal void OpenWriteHandle() {
            if(DeviceHandle != null && !DeviceHandle.IsInvalid)
                Release();
            if(!NativeWin32.LockDevice(Path))
                throw new DeviceError(DeviceError.ErrorLevels.DeviceLockFailed);
            IsLocked = true;
            DeviceHandle = NativeWin32.GetFileHandleRaw(Path, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        ///   Release the device
        /// </summary>
        internal void Release() {
            if(DeviceHandle != null && !DeviceHandle.IsInvalid)
                DeviceHandle.Close();
            NativeWin32.UnLockDevice(Path);
            IsLocked = false;
        }
    }
}