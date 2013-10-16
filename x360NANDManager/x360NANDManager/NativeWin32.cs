namespace x360NANDManager {
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    public class NativeWin32 {
        #region Enums

        #region Nested type: DIGCF

        [Flags] private enum DIGCF {
            DIGCFDefault = 0x00000001, // only valid with DIGCF_DEVICEINTERFACE
            DIGCFPresent = 0x00000002,
            DIGCFAllclasses = 0x00000004,
            DIGCFProfile = 0x00000008,
            DIGCFDeviceinterface = 0x00000010,
        }

        #endregion

        #region Nested type: IOCTL

        internal enum IOCTL {
            DiskGetGeometry = 0x70000,
            DiskGetGeometryEX = 0x700A0,
            StorageGetDeviceNumber = 0x2D1080,
            LockVolume = 0x90018,
            UnlockVolume = 0x9001C,
            DismountVolume = 0x90020
        }

        #endregion

        #endregion Enums

        #region Structs

        #region Nested type: DiskGeometry

        [StructLayout(LayoutKind.Sequential)] public struct DiskGeometry {
            public readonly long Cylinders;
            public readonly uint MediaType;
            public readonly uint TracksPerCylinder;
            public readonly uint SectorsPerTrack;
            public readonly uint BytesPerSector;
        }

        #endregion

        #region Nested type: DiskGeometryEX

        [StructLayout(LayoutKind.Sequential)] public struct DiskGeometryEX {
            public readonly DiskGeometry Geometry;
            public readonly ulong DiskSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public readonly byte[] Data;
        }

        #endregion

        #region Nested type: SpClassinstallHeader

        [StructLayout(LayoutKind.Sequential)] internal struct SpClassinstallHeader {
            internal UInt32 cbSize;
            internal UInt32 InstallFunction;
        }

        #endregion

        #region Nested type: SpDeviceInterfaceData

        [StructLayout(LayoutKind.Sequential)] internal struct SpDeviceInterfaceData {
            internal UInt32 cbSize;
            internal Guid InterfaceClassGuid;
            internal UInt32 Flags;
            internal UIntPtr Reserved;
        }

        #endregion

        #region Nested type: SpDeviceInterfaceDetailData

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] internal struct SpDeviceInterfaceDetailData {
            internal UInt32 cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string DevicePath;
        }

        #endregion

        #region Nested type: SpDevinfoData

        [StructLayout(LayoutKind.Sequential)] internal struct SpDevinfoData {
            internal UInt32 cbSize;
            internal Guid ClassGuid;
            internal UInt32 DevInst;
            internal IntPtr Reserved;
        }

        #endregion

        #region Nested type: SpPropchangeParams

        [StructLayout(LayoutKind.Sequential)] internal struct SpPropchangeParams {
            internal SpClassinstallHeader ClassInstallHeader;
            internal UInt32 StateChange;
            internal UInt32 Scope;
            internal UInt32 HwProfile;
        }

        #endregion

        #region Nested type: StorageDeviceNumber

        [StructLayout(LayoutKind.Sequential)] private struct StorageDeviceNumber {
            internal readonly int deviceType;
            internal readonly int deviceNumber;
            internal readonly int partitionNumber;
        }

        #endregion

        #endregion Structs

        #region SetupAPI

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, UInt32 flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(IntPtr classGuid, string enumerator, IntPtr hwndParent, int flags);

        [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, UInt32 memberIndex, ref SpDevinfoData deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr deviceInfoSet, ref SpDevinfoData deviceInfoData, UInt32 property, ref UInt32 propertyRegDataType, IntPtr propertyBuffer, UInt32 propertyBufferSize, ref UInt32 requiredSize);

        [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, UInt32 memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SpDeviceInterfaceData deviceInterfaceData, ref SpDeviceInterfaceDetailData deviceInterfaceDetailData, UInt32 deviceInterfaceDetailDataSize, ref UInt32 requiredSize, ref SpDevinfoData deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] private static extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SpDevinfoData deviceInfoData, ref SpPropchangeParams classInstallParams, int classInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiChangeState(IntPtr deviceInfoSet, [In] ref SpDevinfoData deviceInfoData);

        #endregion SetupAPI

        #region Kernel32

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)] internal static extern SafeFileHandle CreateFile(string fileName, [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess, [MarshalAs(UnmanagedType.U4)] FileShare fileShare, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, int flags, IntPtr template);

        [DllImport("Kernel32.dll", SetLastError = true)] public static extern int CloseHandle(SafeFileHandle hObject);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)] internal static extern bool DeviceIoControl(SafeFileHandle hDevice, int dwIoControlCode, IntPtr lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, out int lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool ReadFile(SafeFileHandle hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)] internal static extern uint SetFilePointer(SafeFileHandle hFile, int lDistanceToMove, out int lpDistanceToMoveHigh, uint dwMoveMethod);

        #endregion Kernel32

        #region Functions

        internal static SafeFileHandle GetFileHandle(string path, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite | FileShare.Delete, FileMode fileMode = FileMode.Open) {
            Main.SendDebug(string.Format("Getting Handle for: {0}", path));
            if(!path.StartsWith("\\\\", StringComparison.Ordinal)) {
                if(path.Length > 1)
                    path = path.Substring(0, 1);
                return GetFileHandleRaw(string.Format("\\\\.\\{0}:", path.ToUpper()), fileAccess, fileShare, fileMode);
            }
            return GetFileHandleRaw(path.ToUpper(), fileAccess, fileShare, fileMode);
        }

        internal static SafeFileHandle GetFileHandleRaw(string path, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite | FileShare.Delete, FileMode fileMode = FileMode.Open) {
            Main.SendDebug(string.Format("Getting Raw Handle for: {0}", path));
            var handle = CreateFile(path.TrimEnd('\\').ToUpper(), fileAccess, fileShare, IntPtr.Zero, fileMode, 0, IntPtr.Zero);
            if(handle.IsInvalid)
                throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.Win32Error);
            Main.SendDebug("OK!");
            return handle;
        }

        internal static int GetDeviceNumber(string devicePath) {
            Main.SendDebug(string.Format("Getting Drive number for Device: {0}", devicePath));
            var handle = GetFileHandle(devicePath);
            var ptrSdn = IntPtr.Zero;
            try {
                int requiredSize;
                var sdn = new StorageDeviceNumber();
                var nBytes = Marshal.SizeOf(sdn);
                ptrSdn = Marshal.AllocHGlobal(nBytes);
                if(DeviceIoControl(handle, (int) IOCTL.StorageGetDeviceNumber, IntPtr.Zero, 0, ptrSdn, nBytes, out requiredSize, IntPtr.Zero)) {
                    sdn = (StorageDeviceNumber) Marshal.PtrToStructure(ptrSdn, typeof(StorageDeviceNumber));
                    return (sdn.deviceType << 8) + sdn.deviceNumber;
                }
                return -1;
            }
            finally {
                Marshal.FreeHGlobal(ptrSdn);
                handle.Close();
            }
        }

        internal static bool LockDevice(string device) {
            Main.SendDebug(string.Format("Locking: {0}", device));
            var ret = false;
            var deviceNumber = GetDeviceNumber(device);
            foreach(var drive in DriveInfo.GetDrives()) {
                try {
                    if(drive.DriveType != DriveType.Network && drive.DriveType != DriveType.CDRom) {
                        var num = GetDeviceNumber(drive.Name);
                        if(num != deviceNumber)
                            continue;
                    }
                    else
                        continue;
                }
                catch(Win32Exception ex) {
                    if(ex.NativeErrorCode != 5)
                        throw;
                    continue;
                }
                var handle = GetFileHandle(drive.Name);
                try {
                    int outsize;
                    if(!DeviceIoControl(handle, (int) IOCTL.LockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out outsize, IntPtr.Zero))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    if(!DeviceIoControl(handle, (int) IOCTL.DismountVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out outsize, IntPtr.Zero))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    Main.SendDebug(string.Format("{0} Successfully locked!", drive.Name));
                    ret = true;
                }
                finally {
                    handle.Close();
                }
            }
            return ret;
        }

        internal static void UnLockDevice(string device) {
            Main.SendDebug(string.Format("Unlocking: {0}", device));
            var deviceNumber = GetDeviceNumber(device);
            foreach(var drive in DriveInfo.GetDrives()) {
                try {
                    if(drive.DriveType != DriveType.Network && drive.DriveType != DriveType.CDRom) {
                        var num = GetDeviceNumber(drive.Name);
                        if(num != deviceNumber)
                            continue;
                    }
                    else
                        continue;
                }
                catch(Win32Exception ex) {
                    if(ex.NativeErrorCode != 5)
                        throw;
                    continue;
                }
                var handle = GetFileHandle(drive.Name);
                try {
                    int outsize;
                    if(!DeviceIoControl(handle, (int) IOCTL.UnlockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out outsize, IntPtr.Zero)) {
                        var err = Marshal.GetLastWin32Error();
                        if(err != 158) // Device already unlocked, ok... skip it?!
                            throw new Win32Exception(err);
                        Main.SendDebug(string.Format("{0} Is Already Unlocked!", drive.Name));
                    }
                    else
                        Main.SendDebug(string.Format("{0} Successfully Unlocked!", drive.Name));
                }
                finally {
                    handle.Close();
                }
            }
        }

        internal static string GetDevicePath(string device) {
            Main.SendDebug(string.Format("Getting Drive path for Device: {0}", device));
            var deviceNumber = GetDeviceNumber(device);
            var diskGUID = new Guid("53F56307-B6BF-11D0-94F2-00A0C91EFB8B");
            var handle = SetupDiGetClassDevs(ref diskGUID, IntPtr.Zero, IntPtr.Zero, (uint) (DIGCF.DIGCFPresent | DIGCF.DIGCFDeviceinterface));
            try {
                if(handle != new IntPtr(-1)) {
                    var success = true;
                    uint i = 0;
                    while(success) {
                        var devinterfacedata = new SpDeviceInterfaceData();
                        devinterfacedata.cbSize = (uint) Marshal.SizeOf(devinterfacedata);
                        success = SetupDiEnumDeviceInterfaces(handle, IntPtr.Zero, ref diskGUID, i, ref devinterfacedata);
                        if(!success)
                            continue;
                        var devinfodata = new SpDevinfoData();
                        devinfodata.cbSize = (uint) Marshal.SizeOf(devinfodata);
                        var devinterfacedetaildata = new SpDeviceInterfaceDetailData { cbSize = IntPtr.Size == 8 ? 8 : (uint) (4 + Marshal.SystemDefaultCharSize) };
                        uint nRequiredSize = 0;
                        if(!SetupDiGetDeviceInterfaceDetail(handle, ref devinterfacedata, ref devinterfacedetaildata, 1000, ref nRequiredSize, ref devinfodata))
                            SetupDiGetDeviceInterfaceDetail(handle, ref devinterfacedata, ref devinterfacedetaildata, nRequiredSize, ref nRequiredSize, ref devinfodata);
                        var devnum = GetDeviceNumber(devinterfacedetaildata.DevicePath);
                        if(devnum == deviceNumber) {
                            SetupDiDestroyDeviceInfoList(handle);
                            return devinterfacedetaildata.DevicePath;
                        }
                        i++;
                    }
                    throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.NoDeviceFound);
                }
            }
            finally {
                SetupDiDestroyDeviceInfoList(handle);
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        internal static bool IsDeviceConnected(int vendorID, int productID) {
            var handle = SetupDiGetClassDevs(IntPtr.Zero, "USB", IntPtr.Zero, (int) (DIGCF.DIGCFPresent | DIGCF.DIGCFAllclasses));
            try {
                if(handle == new IntPtr(-1))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                var success = true;
                uint i = 0;
                while(success) {
                    var dia = new SpDevinfoData();
                    dia.cbSize = (uint) Marshal.SizeOf(dia);
                    success = SetupDiEnumDeviceInfo(handle, i, ref dia);
                    if(success) {
                        uint requiredSize = 0;
                        uint regType = 0;
                        SetupDiGetDeviceRegistryProperty(handle, ref dia, 1, ref regType, IntPtr.Zero, 0, ref requiredSize);
                        var err = Marshal.GetLastWin32Error();
                        if(err != 0x7A)
                            throw new Win32Exception(err); // Expected error didn't occur, something went wrong (we want to know how big buffer we need)
                        var intPtrBuffer = Marshal.AllocHGlobal((int) requiredSize);
                        if(!SetupDiGetDeviceRegistryProperty(handle, ref dia, 1, ref regType, intPtrBuffer, requiredSize, ref requiredSize))
                            throw new Win32Exception(Marshal.GetLastWin32Error()); // UHOH! Something went horribly wrong!!
                        var hardwareID = Marshal.PtrToStringAuto(intPtrBuffer);
                        Marshal.FreeHGlobal(intPtrBuffer);
                        if(hardwareID == null)
                            throw new Exception("hardwareID is null!");
                        hardwareID = hardwareID.ToUpper();
                        if(hardwareID.Contains("VID_" + vendorID.ToString("X04")) && hardwareID.Contains("PID_" + productID.ToString("X04")))
                            return true; //W00t job well done! we found it!!! :D
                    }
                    i++;
                }
                return false;
            }
            finally {
                SetupDiDestroyDeviceInfoList(handle);
            }
        }

        //internal static DiskGeometry GetGeometry(string rawaddr) {
        //    var handle = GetFileHandleRaw(rawaddr);
        //    try {
        //        var ret = GetGeometry(handle);
        //        return ret;
        //    }
        //    finally {
        //        handle.Close();
        //    }
        //}

        //internal static DiskGeometry GetGeometry(SafeFileHandle handle) {
        //    if(handle.IsInvalid)
        //        throw new Win32Exception(6);
        //    var bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DiskGeometry)));
        //    try {
        //            int retSize;
        //            if (!DeviceIoControl(handle, (int)IOCTL.DiskGetGeometry, IntPtr.Zero, 0, bufPtr, Marshal.SizeOf(typeof(DiskGeometry)), out retSize, IntPtr.Zero))
        //                throw new Win32Exception(Marshal.GetLastWin32Error());
        //            return (DiskGeometry) Marshal.PtrToStructure(bufPtr, typeof(DiskGeometry));
        //    }
        //    finally {
        //        Marshal.FreeHGlobal(bufPtr);
        //    }
        //}

        internal static DiskGeometryEX GetGeometryEX(string rawaddr) {
            Main.SendDebug(string.Format("Getting Drive Geometry for Device: {0}", rawaddr));
            var handle = GetFileHandleRaw(rawaddr);
            try {
                return GetGeometryEX(handle);
            }
            finally {
                handle.Close();
            }
        }

        internal static DiskGeometryEX GetGeometryEX(SafeFileHandle handle) {
            if(handle.IsInvalid)
                throw new Win32Exception(6);
            var bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DiskGeometryEX)));
            try {
                int retSize;
                if(!DeviceIoControl(handle, (int) IOCTL.DiskGetGeometryEX, IntPtr.Zero, 0, bufPtr, Marshal.SizeOf(typeof(DiskGeometryEX)), out retSize, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                var geo = (DiskGeometryEX) Marshal.PtrToStructure(bufPtr, typeof(DiskGeometryEX));
                Main.SendDebug(string.Format("MediaType:     0x{0:X}", geo.Geometry.MediaType));
                Main.SendDebug(string.Format("Cylinders:     0x{0:X}", geo.Geometry.Cylinders));
                Main.SendDebug(string.Format("Tracks:        0x{0:X}", geo.Geometry.TracksPerCylinder));
                Main.SendDebug(string.Format("Sectors:       0x{0:X}", geo.Geometry.SectorsPerTrack));
                Main.SendDebug(string.Format("Bytes:         0x{0:X}", geo.Geometry.BytesPerSector));
                Main.SendDebug(string.Format("DiskSize:      0x{0:X}", geo.DiskSize));
                Main.SendDebug(string.Format("Total Sectors: 0x{0:X}", geo.DiskSize / geo.Geometry.BytesPerSector));
                return geo;
            }
            finally {
                Marshal.FreeHGlobal(bufPtr);
            }
        }

        internal static uint SeekOriginToMoveMethod(SeekOrigin origin) {
            switch(origin) {
                case SeekOrigin.Begin:
                    return 0;
                case SeekOrigin.Current:
                    return 1;
                case SeekOrigin.End:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }
        }

        #endregion Functions
    }
}