namespace x360NANDManager {
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
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

        public enum MediaType : uint
        {
            Unknown,
            F51Pt2512,
            F31Pt44512,
            F32Pt88512,
            F320Pt8512,
            F3720512,
            F5360512,
            F5320512,
            F53201024,
            F5180512,
            F5160512,
            RemovableMedia,
            FixedMedia,
            F3120M512,
            F3640512,
            F5640512,
            F5720512,
            F31Pt2512,
            F31Pt231024,
            F51Pt231024,
            F3128Mb512,
            F3230Mb512,
            F8256128,
            F3200Mb512,
            F3240M512,
            F332M512
        }

        #endregion Enums

        #region Structs

        #region Nested type: DiskGeometry

        [StructLayout(LayoutKind.Sequential)] public struct DiskGeometry {
            public long Cylinders;
            public MediaType MediaType;
            public uint TracksPerCylinder;
            public uint SectorsPerTrack;
            public uint BytesPerSector;

            public long DiskSize
            {
                get
                {
                    return Cylinders * TracksPerCylinder * SectorsPerTrack * BytesPerSector;
                }
            }
        }

        #endregion

        [StructLayout(LayoutKind.Sequential)] internal struct DiskGeometryEX
        {
            internal DiskGeometry Geometry;
            internal ulong DiskSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            internal byte[] Data;
        }

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
        
        #endregion Kernel32

        #region Functions

        internal static SafeFileHandle GetFileHandle(string path, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite | FileShare.Delete, FileMode fileMode = FileMode.Open) {
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
                throw new DeviceError(DeviceError.ErrorLevels.Win32Error);
            Main.SendDebug("OK!");
            return handle;
        }

        internal static int GetDeviceNumber(string devicePath) {
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
                var num = GetDeviceNumber(drive.Name);
                if(num != deviceNumber)
                    continue;
                var handle = GetFileHandle(drive.Name);
                try {
                    int outsize;
                    if(!DeviceIoControl(handle, (int) IOCTL.LockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out outsize, IntPtr.Zero))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    if(!DeviceIoControl(handle, (int) IOCTL.DismountVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out outsize, IntPtr.Zero))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
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
            var ret = false;
            var deviceNumber = GetDeviceNumber(device);
            foreach(var drive in DriveInfo.GetDrives()) {
                var num = GetDeviceNumber(drive.Name.Substring(0, 2));
                if(num != deviceNumber)
                    continue;
                var handle = GetFileHandle(drive.Name);
                try {
                    int outsize;
                    if(!DeviceIoControl(handle, (int) IOCTL.UnlockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out outsize, IntPtr.Zero))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    ret = true;
                }
                finally {
                    handle.Close();
                }
            }
            return;
        }

        internal static string GetDevicePath(string device) {
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
                        var devinterfacedetaildata = new SpDeviceInterfaceDetailData {
                                                                                     cbSize = IntPtr.Size == 8 ? 8 : (uint) (4 + Marshal.SystemDefaultCharSize)
                                                                                     };
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
                    throw new DeviceError(DeviceError.ErrorLevels.NoDeviceFound);
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

        internal static DiskGeometry GetGeometry(string rawaddr) {
            var handle = GetFileHandleRaw(rawaddr);
            try {
                var ret = GetGeometry(handle);
                return ret;
            }
            finally {
                handle.Close();
            }
        }

        internal static DiskGeometry GetGeometry(SafeFileHandle handle) {
            if(handle.IsInvalid)
                throw new Win32Exception(6);
            var bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DiskGeometry)));
            try {
                //using(var deviceIoOverlapped = new DeviceIoOverlapped(new ManualResetEvent(false).SafeWaitHandle.DangerousGetHandle())) {
                    int retSize;
                    //if (!DeviceIoControl(handle, (int)IOCTL.DiskGetGeometry, IntPtr.Zero, 0, bufPtr, Marshal.SizeOf(typeof(DiskGeometry)), out retSize, deviceIoOverlapped.GlobalOverlapped))
                    if (!DeviceIoControl(handle, (int)IOCTL.DiskGetGeometry, IntPtr.Zero, 0, bufPtr, Marshal.SizeOf(typeof(DiskGeometry)), out retSize, IntPtr.Zero))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    return (DiskGeometry) Marshal.PtrToStructure(bufPtr, typeof(DiskGeometry));
                //}
            }
            finally {
                Marshal.FreeHGlobal(bufPtr);
            }
        }

        internal static DiskGeometryEX GetGeometryEX(SafeFileHandle handle)
        {
            if (handle.IsInvalid)
                throw new Win32Exception(6);
            var bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DiskGeometryEX)));
            try
            {
                //using(var deviceIoOverlapped = new DeviceIoOverlapped(new ManualResetEvent(false).SafeWaitHandle.DangerousGetHandle())) {
                int retSize;
                //if (!DeviceIoControl(handle, (int)IOCTL.DiskGetGeometry, IntPtr.Zero, 0, bufPtr, Marshal.SizeOf(typeof(DiskGeometry)), out retSize, deviceIoOverlapped.GlobalOverlapped))
                if (!DeviceIoControl(handle, (int)IOCTL.DiskGetGeometryEX, IntPtr.Zero, 0, bufPtr, Marshal.SizeOf(typeof(DiskGeometryEX)), out retSize, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                var geo = (DiskGeometryEX)Marshal.PtrToStructure(bufPtr, typeof(DiskGeometryEX));
                Main.SendDebug(string.Format("Cylinders: 0x{0:X}", geo.Geometry.Cylinders));
                Main.SendDebug(string.Format("MediaType: 0x{0:X}", geo.Geometry.MediaType));
                Main.SendDebug(string.Format("Tracks:    0x{0:X}", geo.Geometry.TracksPerCylinder));
                Main.SendDebug(string.Format("Sectors:   0x{0:X}", geo.Geometry.SectorsPerTrack));
                Main.SendDebug(string.Format("Bytes:     0x{0:X}", geo.Geometry.BytesPerSector));
                Main.SendDebug(string.Format("DiskSize:  0x{0:X}", geo.DiskSize));
                return geo;
                //}
            }
            finally
            {
                Marshal.FreeHGlobal(bufPtr);
            }
        }

        internal static long TranslateGeometryToSize(DiskGeometry gem) {
            Main.SendDebug(string.Format("Cylinders: {0}", gem.Cylinders));
            Main.SendDebug(string.Format("Tracks: {0}", gem.TracksPerCylinder));
            Main.SendDebug(string.Format("Sectors: {0}", gem.SectorsPerTrack));
            Main.SendDebug(string.Format("Bytes: {0}", gem.BytesPerSector));
            Main.SendDebug(string.Format("DiskSize: {0}", gem.DiskSize));
            return gem.DiskSize;
        }

        #endregion Functions

        //#region Nested type: DeviceIoOverlapped

        //private sealed class DeviceIoOverlapped : IDisposable {
        //    private readonly int _mFieldOffsetEventHandle;
        //    private readonly int _mFieldOffsetInternalHigh;
        //    private readonly int _mFieldOffsetInternalLow;
        //    private readonly int _mFieldOffsetOffsetHigh;
        //    private readonly int _mFieldOffsetOffsetLow;
        //    private IntPtr _mPtrOverlapped = IntPtr.Zero;

        //    public DeviceIoOverlapped(IntPtr hEventOverlapped = default(IntPtr)) {
        //        _mPtrOverlapped = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeOverlapped)));
        //        _mFieldOffsetInternalLow = Marshal.OffsetOf(typeof(NativeOverlapped), "InternalLow").ToInt32();
        //        _mFieldOffsetInternalHigh = Marshal.OffsetOf(typeof(NativeOverlapped), "InternalHigh").ToInt32();
        //        _mFieldOffsetOffsetLow = Marshal.OffsetOf(typeof(NativeOverlapped), "OffsetLow").ToInt32();
        //        _mFieldOffsetOffsetHigh = Marshal.OffsetOf(typeof(NativeOverlapped), "OffsetHigh").ToInt32();
        //        _mFieldOffsetEventHandle = Marshal.OffsetOf(typeof(NativeOverlapped), "EventHandle").ToInt32();
        //        if(hEventOverlapped != IntPtr.Zero)
        //            ClearAndSetEvent(hEventOverlapped);
        //    }

        //    private IntPtr InternalLow {
        //        set { Marshal.WriteIntPtr(_mPtrOverlapped, _mFieldOffsetInternalLow, value); }
        //    }

        //    private IntPtr InternalHigh {
        //        set { Marshal.WriteIntPtr(_mPtrOverlapped, _mFieldOffsetInternalHigh, value); }
        //    }

        //    private int OffsetLow {
        //        set { Marshal.WriteInt32(_mPtrOverlapped, _mFieldOffsetOffsetLow, value); }
        //    }

        //    private int OffsetHigh {
        //        set { Marshal.WriteInt32(_mPtrOverlapped, _mFieldOffsetOffsetHigh, value); }
        //    }

        //    /// <summary>
        //    ///   The overlapped event wait handle.
        //    /// </summary>
        //    private IntPtr EventHandle {
        //        set { Marshal.WriteIntPtr(_mPtrOverlapped, _mFieldOffsetEventHandle, value); }
        //    }

        //    /// <summary>
        //    ///   Pass this into the DeviceIoControl and GetOverlappedResult APIs
        //    /// </summary>
        //    public IntPtr GlobalOverlapped {
        //        get { return _mPtrOverlapped; }
        //    }

        //    #region IDisposable Members

        //    public void Dispose() {
        //        GC.SuppressFinalize(this);
        //        if(_mPtrOverlapped == IntPtr.Zero)
        //            return;
        //        Marshal.FreeHGlobal(_mPtrOverlapped);
        //        _mPtrOverlapped = IntPtr.Zero;
        //    }

        //    #endregion IDisposable Members

        //    /// <summary>
        //    ///   Set the overlapped wait handle and clear out the rest of the structure.
        //    /// </summary>
        //    /// <param name="hEventOverlapped"> </param>
        //    public void ClearAndSetEvent(IntPtr hEventOverlapped) {
        //        EventHandle = hEventOverlapped;
        //        InternalLow = IntPtr.Zero;
        //        InternalHigh = IntPtr.Zero;
        //        OffsetLow = 0;
        //        OffsetHigh = 0;
        //    }

        //    ~DeviceIoOverlapped() {
        //        if(_mPtrOverlapped == IntPtr.Zero)
        //            return;
        //        Marshal.FreeHGlobal(_mPtrOverlapped);
        //        _mPtrOverlapped = IntPtr.Zero;
        //    }
        //}

        //#endregion Nested type: DeviceIoOverlapped
    }
}