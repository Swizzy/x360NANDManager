namespace x360NANDManager.SPI {
    using System;
    using LibUsbDotNet;
    using LibUsbDotNet.Main;

    internal abstract class ARMBase {
        public uint ArmVersion;
        internal bool Initialized;
        private UsbDevice _device;
        private int _productID;
        private UsbEndpointReader _reader;
        private int _vendorID;
        private UsbEndpointWriter _writer;
        protected internal uint Status { get; protected set; }

        ~ARMBase() {
            Release();
        }

        private static void UsbDeviceOnUsbErrorEvent(object sender, UsbError usbError) {
            Main.SendDebug(String.Format("A USB Error Occured: {1}{0}", usbError, Environment.NewLine));
        }

        private void Reset() {
            if(!Initialized || _device == null)
                return;
            Main.SendDebug("Device Reset Started...");
            var wholeUsbDevice = _device as IUsbDevice;
            if(ReferenceEquals(wholeUsbDevice, null))
                return;
            wholeUsbDevice.ResetDevice();
            wholeUsbDevice.SetConfiguration(1);
            Main.SendDebug("Device Reset Completed...");
        }

        internal void SendCMD(Commands cmd, uint argA = 0, uint argB = 0) {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return;
            }
            Main.SendDebug(String.Format("Sending CMD: {0} (0x{0:X}) 0x{1:X08} 0x{2:X08}", cmd, argA, argB));
            var buf = BitConverter.GetBytes(argA);
            var tmp = BitConverter.GetBytes(argB);
            Array.Resize(ref buf, buf.Length + tmp.Length);
            Array.Copy(tmp, 0, buf, buf.Length - tmp.Length, tmp.Length);
            var packet = new UsbSetupPacket((byte) UsbRequestType.TypeVendor, (byte) cmd, 0, 0, 0);
            int sent;
            _device.ControlTransfer(ref packet, buf, buf.Length, out sent);
        }

        internal byte[] FlashRead(uint block, bool verboseError = false) {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return new byte[0];
            }
            SendCMD(Commands.DataRead, block, 0x4200);
            var ret = new byte[0x4200];
            var totalread = 0;
            var err = ErrorCode.None;
            var tries = 0;
            while(totalread < ret.Length && tries < 10) {
                int read;
                var tmp = new byte[0x4200 - totalread];
                err = _reader.Read(tmp, 1000, out read);
                if(err != ErrorCode.None && err != ErrorCode.IoTimedOut)
                    Main.SendDebug(String.Format("Error: {0}", err));
                if(read != 0x4200)
                    Buffer.BlockCopy(tmp, 0, ret, totalread, tmp.Length);
                else
                    ret = tmp;
                totalread += read;
                tries++;
            }
            if(err == ErrorCode.None) {
                GetFlashStatus();
                Utils.IsBadBlock(Status, block, "Reading", verboseError);
            }
            return err == ErrorCode.None ? ret : new byte[0];
        }

        protected bool FlashErase(uint block) {
            if(!Initialized)
                return false;
            SendCMD(Commands.DataErase, block, 0x4);
            return true;
        }

        protected bool FlashWrite(uint block, byte[] buf) {
            if(buf.Length != 0x4200 || !Initialized)
                return false;
            SendCMD(Commands.DataWrite, block, (uint) buf.Length);
            var totalWrote = 0;
            var err = ErrorCode.None;
            var tries = 0;
            while(totalWrote < buf.Length && tries < 10) {
                int wrote;
                if(totalWrote > 0) {
                    var tmp = new byte[buf.Length - totalWrote];
                    Buffer.BlockCopy(buf, totalWrote, tmp, 0, tmp.Length);
                    err = _writer.Write(tmp, 1000, out wrote);
                }
                else
                    err = _writer.Write(buf, 1000, out wrote);
                if(err != ErrorCode.None)
                    Main.SendDebug(String.Format("Error: {0}", err));
                totalWrote += wrote;
                tries++;
            }
            return err == ErrorCode.None;
        }

        protected bool DeviceInit(int vendorID, int productID, bool reset = true) {
            try {
                _vendorID = vendorID;
                _productID = productID;
                _device = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vendorID, productID));
                if(_device == null) {
                    Main.SendDebug("No Device Found!");
                    Release();
                    return false;
                }
                if(reset) {
                    Reset();
                    return DeviceInit(vendorID, productID, false);
                }
                var wholeUsbDevice = _device as IUsbDevice;
                if(!ReferenceEquals(wholeUsbDevice, null)) {
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                }
                _reader = _device.OpenEndpointReader((ReadEndpointID) 0x82);
                _writer = _device.OpenEndpointWriter((WriteEndpointID) 0x05);
                UsbDevice.UsbErrorEvent += UsbDeviceOnUsbErrorEvent;
                Initialized = true;
                SendCMD(Commands.DevVersion, 0, 4);
                ArmVersion = ReadUInt32();
                Main.SendDebug(string.Format("Arm Version: {0}", ArmVersion));
                return true;
            }
            catch(Exception ex) {
                Main.SendDebug(String.Format("Device Init exception occured: {0}", ex.Message));
                return false;
            }
        }

        internal void Release() {
            Initialized = false;
            if(_device != null) {
                Reset();
                var wholeUsbDevice = _device as IUsbDevice;
                if(!ReferenceEquals(wholeUsbDevice, null))
                    wholeUsbDevice.ReleaseInterface(1);
                _device.Close();
                _device = null;
            }
            UsbDevice.UsbErrorEvent -= UsbDeviceOnUsbErrorEvent;
            UsbDevice.Exit();
        }

        private uint ReadUInt32() {
            var buf = new byte[4];
            var totalread = 0;
            var err = ErrorCode.None;
            var tries = 0;
            while(totalread < buf.Length && tries < 10) {
                int read;
                var tmp = new byte[buf.Length - totalread];
                err = _reader.Read(tmp, 1000, out read);
                if(err != ErrorCode.None && err != ErrorCode.IoTimedOut)
                    Main.SendDebug(String.Format("Error: {0}", err));
                if(read < buf.Length)
                    Buffer.BlockCopy(tmp, 0, buf, totalread, tmp.Length);
                else
                    buf = tmp;
                totalread += read;
                tries++;
            }
            var val = BitConverter.ToUInt32(buf, 0);
            if(err != ErrorCode.None) {
                if(err == ErrorCode.IoTimedOut)
                    Release();
                Main.SendDebug(String.Format("ReadUInt32 Failed! Error: {0} Value read: {1}", err, val));
                return 0;
            }
            return val;
        }

        private uint GetARMStatus(Commands cmd) {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return 0;
            }
            SendCMD(cmd);
            return GetARMStatus();
        }

        internal uint GetARMStatus() {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return 0;
            }
            return ReadUInt32();
        }

        internal void GetFlashStatus() {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return;
            }
            SendCMD(Commands.DataStatus, 0, 0x4);
            Status = GetARMStatus();
        }

        public void SetXboxPowerState(bool poweron) {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return;
            }
            SendCMD(poweron ? Commands.XboxPwron : Commands.XboxPwroff);
        }

        public uint FlashInit() {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return 0;
            }
            return GetARMStatus(Commands.DataInit);
        }

        public void FlashDeInit() {
            if(!Initialized) {
                Main.SendError("Not Initialized!");
                return;
            }
            SendCMD(Commands.DataDeinit);
            _reader.ReadFlush();
        }

        public uint DeviceCycle() {
            FlashDeInit();
            Release();
            DeviceInit(_vendorID, _productID, false);
            return FlashInit();
        }

        #region Nested type: Commands

        internal enum Commands : byte {
            DataRead = 0x01,
            DataWrite = 0x02,
            DataInit = 0x03,
            DataDeinit = 0x04,
            DataStatus = 0x05,
            DataErase = 0x06,
            DataExec = 0x07,
            DevVersion = 0x08,
            XSVFExec = 0x09,
            XboxPwron = 0x10,
            XboxPwroff = 0x11,
            DevUpdate = 0xF0
        }

        #endregion
    }
}