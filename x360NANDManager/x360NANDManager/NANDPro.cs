namespace x360NANDManager {
    using System;
    using LibUsbDotNet;
    using LibUsbDotNet.Main;

    internal static class NANDPro {
        static NANDPro() {
            Initialized = Init();
        }

        //~NANDPro() {
        //    Release();
        //}

        #region Constants

        private const byte CMDDataRead = 0x01;
        private const byte CMDDataWrite = 0x02;
        private const byte CMDDataInit = 0x03;
        private const byte CMDDataDeinit = 0x04;
        private const byte CMDDataStatus = 0x05;
        private const byte CMDDataErase = 0x06;
        private const byte CMDDataExec = 0x07;
        private const byte CMDDevVersion = 0x08;
        private const byte CMDXSVFExec = 0x09;
        private const byte CMDXboxPwron = 0x10;
        private const byte CMDXboxPwroff = 0x11;
        private const byte CMDDevUpdate = 0xF0;

        #endregion Constants

        private static UsbDevice _device;
        private static readonly UsbDeviceFinder DeviceFinder = new UsbDeviceFinder(0xffff, 0x4);
        private static UsbEndpointReader _reader;
        private static UsbEndpointWriter _writer;
        internal static bool Initialized { get; private set; }

        public static uint Status { get; private set; }
        public static uint ArmVersion;

        private static void UsbDeviceOnUsbErrorEvent(object sender, UsbError usbError) {
            Main.SendError(string.Format("A USB Error Occured: {0}", usbError));
            var endpointBase = sender as UsbEndpointBase;
            if(endpointBase == null || usbError.Win32ErrorNumber != 0x1F || _device == null || !_device.IsOpen || !endpointBase.Reset())
                return;
            Main.SendError("USB Endpoint Successfully reset");
        }
        
        internal static void ResetStatus() {
            Status = 0;
        }

        public static bool Init() {
            Initialized = false;
            if(_device != null && _device.IsOpen)
                Release();
            try {
                _device = UsbDevice.OpenUsbDevice(DeviceFinder);
                if (_device == null)
                    return false;
                var wholeUsbDevice = _device as IUsbDevice;
                if(!ReferenceEquals(wholeUsbDevice, null)) {
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                }
                _reader = _device.OpenEndpointReader((ReadEndpointID)0x82);
                _writer = _device.OpenEndpointWriter((WriteEndpointID)0x05);
                UsbDevice.UsbErrorEvent += UsbDeviceOnUsbErrorEvent;
                Initialized = true;
                return true;
            }
            catch(Exception ex) {
                Main.SendError(string.Format("Device Init exception occured: {0}", ex.Message));
                return false;
            }
        }

        public static void Release() {
            if(_device == null) {
                UsbDevice.UsbErrorEvent -= UsbDeviceOnUsbErrorEvent;
                Initialized = false;
                return;
            }
            var wholeUsbDevice = _device as IUsbDevice;
            if(!ReferenceEquals(wholeUsbDevice, null))
                wholeUsbDevice.ReleaseInterface(0);
            _device.Close();
            _device = null;
            UsbDevice.UsbErrorEvent -= UsbDeviceOnUsbErrorEvent;
            UsbDevice.Exit();
            Initialized = false;
        }

        private static void Reset() {
            if(!Initialized)
                return;
            var wholeUsbDevice = _device as IUsbDevice;
            if(ReferenceEquals(wholeUsbDevice, null))
                return;
            wholeUsbDevice.ResetDevice();
            wholeUsbDevice.SetConfiguration(1);
        }

        private static void SendCMD(byte cmd, uint argA = 0, uint argB = 0) {
            if(!Initialized)
                return;
            var buf = BitConverter.GetBytes(argA);
            var tmp = BitConverter.GetBytes(argB);
            Array.Resize(ref buf, buf.Length + tmp.Length);
            Array.Copy(tmp, 0, buf, buf.Length - tmp.Length, tmp.Length);
            var packet = new UsbSetupPacket((byte) UsbRequestType.TypeVendor, cmd, 0, 0, 0);
            int sent;
            _device.ControlTransfer(ref packet, buf, buf.Length, out sent);
        }

        private static uint ReadUInt32() {
            var buf = new byte[4];
            int read;
            var err = _reader.Read(buf, 1000, out read);
            return err == ErrorCode.Success ? BitConverter.ToUInt32(buf, 0) : 0;
        }

        public static uint GetARMVersion() {
            if(!Initialized)
                return 0;
            SendCMD(CMDDevVersion, 0, 4);
            ArmVersion = ReadUInt32();
            return ArmVersion;
        }

        public static void EnterUpdateMode() {
            if(!Initialized)
                return;
            try {
                SendCMD(CMDDevUpdate);
            }
            catch(Exception) {
            }
        }

        public static bool XSVFInit() {
            if(!Initialized)
                return false;
            Reset();
            return GetARMVersion() == 3;
        }

        public static void XSVFWrite(byte[] buf) {
            if(!Initialized)
                return;
            SendCMD(CMDDataWrite, 0, (uint) buf.Length);
            int wrote;
            _writer.Write(buf, 10000, out wrote);
        }

        public static void XSVFExecute() {
            if(!Initialized)
                return;
            SendCMD(CMDXSVFExec);
            Status = GetARMStatus();
        }

        private static uint GetARMStatus(byte cmd) {
            if(!Initialized)
                return 0;
            SendCMD(cmd);
            return GetARMStatus();
        }

        private static uint GetARMStatus() {
            return !Initialized ? 0 : ReadUInt32();
        }

        public static uint FlashInit() {
            GetARMVersion();
            return GetARMStatus(CMDDataInit);
        }

        public static void FlashDeInit() {
            Status = GetARMStatus(CMDDataDeinit);
        }

        private static void GetFlashStatus() {
            Status = GetARMStatus(CMDDataStatus);
        }

        public static void FlashErase(uint block) {
            if(!Initialized)
                return;
            SendCMD(CMDDataErase, block);
            GetFlashStatus();
            if (ArmVersion >= 3)
                SendCMD(CMDDataExec, block);
        }

        public static byte[] FlashRead(uint block) {
            if(!Initialized)
                return new byte[0];
            SendCMD(CMDDataRead, block, 0x4200);
            var ret = new byte[0x4200];
            int read;
            var err = _reader.Read(ret, 1000, out read);
            GetFlashStatus();
            return err == ErrorCode.None ? ret : new byte[0];
        }

        public static bool FlashWrite(uint block, byte[] buf) {
            if(buf.Length != 0x4200 || !Initialized)
                return false;
            SendCMD(CMDDataWrite, block, (uint)buf.Length);
            int wrote;
            var err = _writer.Write(buf, 1000, out wrote);
            if (wrote != buf.Length)
                Main.SendError("Code Error Inside...");
            GetFlashStatus();
            if (ArmVersion >= 3)
                SendCMD(CMDDataExec, block);
            return err == ErrorCode.None;
        }

        public static void SetXboxPowerState(bool poweron) {
            SendCMD(poweron ? CMDXboxPwron : CMDXboxPwroff);
        }
    }
}