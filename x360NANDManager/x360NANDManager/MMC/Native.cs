namespace x360NANDManager.MMC {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    internal class Native {
        internal MMCError LastError { get; private set; }

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
            DiskGetMediaTypes = 0x70000,
            StorageGetDeviceNumber = 0x2D1080,
            LockVolume = 0x90018,
            UnlockVolume = 0x9001C,
            DismountVolume = 0x90020
        }

        #endregion

        #endregion Enums

        #region Structs

        #region Nested type: SpClassinstallHeader

        [StructLayout(LayoutKind.Sequential)] internal struct SpClassinstallHeader {
            internal UInt32 cbSize;
            internal UInt32 InstallFunction;
        }

        #endregion

        #region Nested type: SpDeviceInterfaceData

        [StructLayout(LayoutKind.Sequential)] internal struct SpDeviceInterfaceData {
            public UInt32 cbSize;
            public Guid InterfaceClassGuid;
            public UInt32 Flags;
            public UIntPtr Reserved;
        }

        #endregion

        #region Nested type: SpDeviceInterfaceDetailData

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] internal struct SpDeviceInterfaceDetailData {
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DevicePath;
        }

        #endregion

        #region Nested type: SpDevinfoData

        [StructLayout(LayoutKind.Sequential)] internal struct SpDevinfoData {
            public UInt32 cbSize;
            public Guid ClassGuid;
            public UInt32 DevInst;
            public IntPtr Reserved;
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
            private readonly int partitionNumber;
        }

        #endregion

        #endregion Structs

        #region SetupAPI

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] internal static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, UInt32 flags);

        [DllImport("setupapi.dll", SetLastError = true)] internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] internal static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, UInt32 memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] internal static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SpDeviceInterfaceData deviceInterfaceData, ref SpDeviceInterfaceDetailData deviceInterfaceDetailData, UInt32 deviceInterfaceDetailDataSize, out UInt32 requiredSize, ref SpDevinfoData deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] internal static extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SpDevinfoData deviceInfoData, ref SpPropchangeParams classInstallParams, int classInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiChangeState(IntPtr deviceInfoSet, [In] ref SpDevinfoData deviceInfoData);

        #endregion SetupAPI

        #region Kernel32

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)] internal static extern SafeFileHandle CreateFile(string fileName, [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess, [MarshalAs(UnmanagedType.U4)] FileShare fileShare, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, int flags, IntPtr template);

        [DllImport("Kernel32.dll", SetLastError = true)] public static extern int CloseHandle(SafeFileHandle hObject);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)] internal static extern bool DeviceIoControl(SafeFileHandle hDevice, int dwIoControlCode, IntPtr lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);

        #endregion Kernel32

        #region Functions

        internal bool GetFileHandle(string path, out SafeFileHandle handle, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.Read, FileMode fileMode = FileMode.OpenOrCreate) {
            if(path.Length > 1)
                path = path.Substring(0, 1);
            return GetFileHandleRaw(string.Format("\\\\.\\{0}:", path.ToUpper()), out handle, fileAccess, fileShare, fileMode);
        }

        internal bool GetFileHandleRaw(string path, out SafeFileHandle handle, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.Read, FileMode fileMode = FileMode.OpenOrCreate) {
            handle = CreateFile(path.TrimEnd('\\').ToUpper(), fileAccess, fileShare, IntPtr.Zero, fileMode, 0, IntPtr.Zero);
            if(handle.IsInvalid) {
                LastError = new MMCError(MMCError.ErrorLevels.Win32Error);
                return false;
            }
            return true;
        }

        internal int GetDeviceNumber(string devicePath) {
            SafeFileHandle h = null;
            var ptrSdn = IntPtr.Zero;
            try {
                if(GetFileHandle(devicePath, out h)) {
                    var requiredSize = 0;
                    var sdn = new StorageDeviceNumber();
                    var nBytes = Marshal.SizeOf(sdn);
                    ptrSdn = Marshal.AllocHGlobal(nBytes);
                    if(DeviceIoControl(h, (int) IOCTL.StorageGetDeviceNumber, IntPtr.Zero, 0, ptrSdn, nBytes, ref requiredSize, IntPtr.Zero)) {
                        sdn = (StorageDeviceNumber) Marshal.PtrToStructure(ptrSdn, typeof(StorageDeviceNumber));
                        return (sdn.deviceType << 8) + sdn.deviceNumber;
                    }
                }
                return -1;
            }
            finally {
                Marshal.FreeHGlobal(ptrSdn);
                CloseHandle(h);
            }
        }

        internal bool LockDevice(string device) {
            var ret = false;
            var deviceNumber = GetDeviceNumber(device);
            foreach(var drive in DriveInfo.GetDrives()) {
                var num = GetDeviceNumber(drive.Name);
                if(num != deviceNumber)
                    continue;
                SafeFileHandle handle;
                if(!GetFileHandle(drive.Name, out handle))
                    return false;
                var outsize = 0;
                if(!DeviceIoControl(handle, (int) IOCTL.LockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, ref outsize, IntPtr.Zero)) {
                    if(Marshal.GetLastWin32Error() == 0) {
                        ret = true;
                        continue;
                    }
                    return false;
                }
                if(!DeviceIoControl(handle, (int) IOCTL.DismountVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, ref outsize, IntPtr.Zero))
                    return false;
                ret = true;
            }
            return ret;
        }

        internal bool UnLockDevice(string device) {
            var ret = false;
            var deviceNumber = GetDeviceNumber(device);
            foreach(var drive in DriveInfo.GetDrives()) {
                var num = GetDeviceNumber(drive.Name.Substring(0, 2));
                if(num != deviceNumber)
                    continue;
                SafeFileHandle handle;
                if(!GetFileHandle(drive.Name, out handle))
                    return false;
                var outsize = 0;
                if(!DeviceIoControl(handle, (int) IOCTL.UnlockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, ref outsize, IntPtr.Zero))
                    return false;
                ret = true;
            }
            return ret;
        }

        internal string GetDevicePath(string device) {
            var deviceNumber = GetDeviceNumber(device);
            var diskGUID = new Guid("53F56307-B6BF-11D0-94F2-00A0C91EFB8B");
            var h = SetupDiGetClassDevs(ref diskGUID, IntPtr.Zero, IntPtr.Zero, (uint) (DIGCF.DIGCFPresent | DIGCF.DIGCFDeviceinterface));
            if(h != new IntPtr(-1)) {
                var success = true;
                uint i = 0;
                while(success) {
                    var dia = new SpDeviceInterfaceData();
                    dia.cbSize = (uint) Marshal.SizeOf(dia);
                    success = SetupDiEnumDeviceInterfaces(h, IntPtr.Zero, ref diskGUID, i, ref dia);
                    if(!success)
                        continue;
                    var da = new SpDevinfoData();
                    da.cbSize = (uint) Marshal.SizeOf(da);
                    var didd = new SpDeviceInterfaceDetailData();
                    if(IntPtr.Size == 8) // for 64 bit operating systems
                        didd.cbSize = 8;
                    else
                        didd.cbSize = (uint) (4 + Marshal.SystemDefaultCharSize); // for 32 bit systems
                    uint nRequiredSize;
                    uint nBytes = 1000;
                    if(!SetupDiGetDeviceInterfaceDetail(h, ref dia, ref didd, nBytes, out nRequiredSize, ref da))
                        SetupDiGetDeviceInterfaceDetail(h, ref dia, ref didd, nRequiredSize, out nRequiredSize, ref da);
                    var devnum = GetDeviceNumber(didd.DevicePath);
                    if(devnum == deviceNumber) {
                        SetupDiDestroyDeviceInfoList(h);
                        return didd.DevicePath;
                    }
                    i++;
                }
            }
            LastError = new MMCError(MMCError.ErrorLevels.Win32Error);
            SetupDiDestroyDeviceInfoList(h);
            return "";
        }

        #region Disk Geometry



        #endregion Disk Geometry

        #endregion Functions
    }
}