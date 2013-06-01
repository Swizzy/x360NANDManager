namespace x360NANDManager.SPI {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using LibUsbDotNet;
    using LibUsbDotNet.Main;

    internal sealed class ARMFlasher : FlasherOutput, ISPIFlasher {
        public uint ArmVersion;
        private UsbDevice _device;
        private bool _flashInitialized;
        private int _productID;
        private UsbEndpointReader _reader;
        private int _vendorID;
        private UsbEndpointWriter _writer;
        private XConfig _xcfg;
        private bool _abort;

        public ARMFlasher() {
            if(DeviceInit(0xFFFF, 0x4) || DeviceInit(0x11D4, 0x8338))
                return;
            Release();
            throw new DeviceError(DeviceError.ErrorLevels.NoDeviceFound);
        }

        ~ARMFlasher() {
            Release();
        }

        private void UsbDeviceOnUsbErrorEvent(object sender, UsbError usbError) {
            Main.SendDebug(String.Format("A USB Error Occured: {0}", usbError));
            if(_device == null || !_device.IsOpen)
                throw new DeviceError(DeviceError.ErrorLevels.DeviceCrashed);
        }

        private void CheckDeviceState() {
            if(_device == null || !_device.IsOpen)
                throw new DeviceError(DeviceError.ErrorLevels.DeviceNotInitialized);
        }

        private void CheckFlashState() {
            CheckDeviceState();
            if(!_flashInitialized)
                throw new DeviceError(DeviceError.ErrorLevels.FlashNotInitialized);
        }

        private void SendCMD(Commands cmd, uint argA = 0, uint argB = 0) {
            CheckDeviceState();
            Main.SendDebug(String.Format("Sending CMD: {0} (0x{0:X}) 0x{1:X08} 0x{2:X08}", cmd, argA, argB));
            var buf = BitConverter.GetBytes(argA);
            var tmp = BitConverter.GetBytes(argB);
            Array.Resize(ref buf, buf.Length + tmp.Length);
            Array.Copy(tmp, 0, buf, buf.Length - tmp.Length, tmp.Length);
            var packet = new UsbSetupPacket((byte) UsbRequestType.TypeVendor, (byte) cmd, 0, 0, 0);
            int sent;
            _device.ControlTransfer(ref packet, buf, buf.Length, out sent);
        }

        private bool DeviceInit(int vendorID, int productID, bool reset = true) {
            try {
                _device = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vendorID, productID));
                if(_device == null) {
                    Main.SendDebug(string.Format("No Device Found with VendorID: 0x{0:X04} and ProductID: 0x{1:X04}", vendorID, productID));
                    Release();
                    return false;
                }
                var wholeUsbDevice = _device as IUsbDevice;
                if(!ReferenceEquals(wholeUsbDevice, null)) {
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                }
                if (reset)
                {
                    DeviceReset();
                    return DeviceInit(vendorID, productID, false);
                }
                _reader = _device.OpenEndpointReader((ReadEndpointID) 0x82);
                _reader.ReadFlush();
                _writer = _device.OpenEndpointWriter((WriteEndpointID) 0x05);
                UsbDevice.UsbErrorEvent += UsbDeviceOnUsbErrorEvent;
                SendCMD(Commands.DevVersion, 0, 4);
                ArmVersion = ReadUInt32();
                UpdateStatus(string.Format("Arm Version: {0}", ArmVersion));
                _vendorID = vendorID;
                _productID = productID;
                return true;
            }
            catch(Exception ex) {
                Main.SendDebug(String.Format("Device Init exception occured: {0}", ex.Message));
                throw;
            }
        }

        private void DeviceReset() {
            if(_device == null || !_device.IsOpen)
                return;
            var wholeUsbDevice = _device as IUsbDevice;
            if(ReferenceEquals(wholeUsbDevice, null))
                return;
            wholeUsbDevice.ResetDevice();
            wholeUsbDevice.SetConfiguration(1);
            Main.SendDebug("Device Successfully reset!");
        }

        private ErrorCode ReadFromDevice(byte[] buf, int tries = 10) {
            var totalread = 0;
            var err = ErrorCode.None;
            while(totalread < buf.Length && tries > 0) {
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
                tries--;
            }
            return tries > 0 ? ErrorCode.Success : err;
        }

        private ErrorCode WriteToDevice(byte[] buf, int tries = 10) {
            if(buf == null)
                throw new ArgumentNullException("buf");
            var totalwrote = 0;
            var err = ErrorCode.None;
            while(totalwrote > buf.Length && tries > 0) {
                int wrote;
                if(totalwrote > 0) {
                    var tmp = new byte[buf.Length - totalwrote];
                    Buffer.BlockCopy(buf, buf.Length - tmp.Length, tmp, 0, tmp.Length);
                    err = _writer.Write(tmp, 1000, out wrote);
                }
                else
                    err = _writer.Write(buf, 1000, out wrote);
                if(err != ErrorCode.None && err != ErrorCode.IoTimedOut)
                    Main.SendDebug(String.Format("Error: {0}", err));
                totalwrote += wrote;
                tries--;
            }
            return tries > 0 ? ErrorCode.Success : err;
        }

        private uint GetARMStatus(Commands cmd) {
            CheckDeviceState();
            SendCMD(cmd);
            return GetARMStatus();
        }

        private uint GetARMStatus() {
            CheckDeviceState();
            return ReadUInt32();
        }

        private uint ReadUInt32() {
            CheckDeviceState();
            var buf = new byte[4];
            var err = ReadFromDevice(buf);
            var val = BitConverter.ToUInt32(buf, 0);
            if(err != ErrorCode.None) {
                Main.SendDebug(String.Format("ReadUInt32 Failed! Error: {0} Value read: {1}", err, val));
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            }
            return val;
        }

        private uint GetFlashStatus(uint block) {
            CheckFlashState();
            SendCMD(Commands.DataStatus, block, 0x4);
            return ReadUInt32();
        }

        #region Implementation of IFlasher

        /// <summary>
        ///   Initialize the flash before operations start
        ///   <exception cref="DeviceError">If Device is not initalized</exception>
        ///   <exception cref="ArgumentException">If flashconfig is invalid</exception>
        /// </summary>
        /// <param name="config"> Flashconfig information (Information about the consoles memory) </param>
        public void Init(out XConfig config) {
            CheckDeviceState();
            _xcfg = new XConfig(GetARMStatus(Commands.DataInit));
            config = _xcfg;
            _flashInitialized = true;
        }

        /// <summary>
        ///   DeInitalize flash after operations complete
        ///   <exception cref="DeviceError">If Device is not initalized</exception>
        /// </summary>
        public void DeInit() {
            CheckDeviceState();
            SendCMD(Commands.DataDeinit);
            _reader.ReadFlush();
            _flashInitialized = false;
        }

        /// <summary>
        ///   Release the USB Device
        /// </summary>
        public void Release() {
            if(_device != null && _device.IsOpen) {
                DeviceReset();
                _device.Close();
                _device = null;
            }
            UsbDevice.UsbErrorEvent -= UsbDeviceOnUsbErrorEvent;
            UsbDevice.Exit();
        }

        /// <summary>
        ///   Cycle device between operations
        /// <exception cref="DeviceError">If there is any problem with the device or the reset fails</exception>
        /// </summary>
        public void Reset() {
            DeInit();
            Release();
            DeviceInit(_vendorID, _productID);
            XConfig tmp;
            Init(out tmp);
            if(_xcfg.Config == tmp.Config)
                throw new DeviceError(DeviceError.ErrorLevels.ResetFailed);
        }

        public void Abort() {
            _abort = true;
        }

        /// <summary>
        ///   Sends the erase command for <paramref name="blockID" />
        /// <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="blockID"/> is greater then total blocks on device</exception>
        /// </summary>
        /// <param name="blockID"> Block ID to erase </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on erase errors or just a write error (default = only print write error without details) </param>
        public void EraseBlock(uint blockID, int verboseLevel = 0) {
            CheckFlashState();
            if (blockID > _xcfg.SizeSmallBlocks)
                throw new ArgumentOutOfRangeException("blockID");
            SendCMD(Commands.DataErase, blockID, 0x4);
            _reader.ReadFlush();
            var status = GetARMStatus();
            IsBadBlock(status, blockID, "Erasing", verboseLevel >= 1);
        }

        /// <summary>
        ///   Sends the erase command for BlockID: <paramref name="startBlock" /> and onwards for <paramref name="blockCount"/>
        /// <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        /// <exception cref="ArgumentException">If there is a problem with your block count settings</exception>
        /// </summary>
        /// <param name="startBlock">Starting blockID</param>
        /// <param name="blockCount">Block count (Small blocks!) if set to 0 full device erase will be done</param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on erase errors or just a write error (default = only print write error without details) </param>
        public void Erase(uint startBlock, uint blockCount, int verboseLevel = 0) {
            CheckDeviceState();
            var sw = Stopwatch.StartNew();
            blockCount = _xcfg.FixBlockCount(startBlock, blockCount);
            var last = startBlock + blockCount;
            UpdateStatus(string.Format("Erasing blocks 0x{0:X} -> 0x{1:X}", startBlock, last));
            for (var block = startBlock; block < last; block ++) {
                if (_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Erase aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                UpdateProgress(block, last);
                EraseBlock(block, verboseLevel);
            }
            if (!_abort) {
                sw.Stop();
                UpdateStatus(string.Format("Erase completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }
            _abort = false;
        }

        /// <summary>
        ///   Writes the data of <paramref name="data" /> to <paramref name="blockID" /> <c>as is</c>
        /// <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="blockID"/> is greater then total blocks on device</exception>
        /// </summary>
        /// <param name="blockID"> Block ID to write to </param>
        /// <param name="data"> Data to write </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on write errors or just a write error (default = only print write error without details) </param>
        public void WriteBlock(uint blockID, byte[] data, int verboseLevel = 0) {
            CheckFlashState();
            if(data.Length != _xcfg.BlockSize)
                throw new ArgumentException(string.Format("Data must be 0x{0:X} bytes in length for this flashconfig!", _xcfg.BlockRawSize));
            if (blockID > _xcfg.SizeSmallBlocks)
                throw new ArgumentOutOfRangeException("blockID");
            SendCMD(Commands.DataWrite, blockID, (uint) data.Length);
            var err = WriteToDevice(data);
            if(err != ErrorCode.Success)
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            var status = GetFlashStatus(blockID);
            IsBadBlock(status, blockID, "Writing", verboseLevel >= 1);
        }

        public void Write(uint startBlock, uint blockCount, byte[] data, SPIWriteModes modes = SPIWriteModes.None, int verboseLevel = 0) {
            CheckDeviceState();
            throw new NotImplementedException();
        }

        public void Write(uint startBlock, uint blockCount, string file, SPIWriteModes modes = SPIWriteModes.None, int verboseLevel = 0) {
            CheckDeviceState();
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Reads <paramref name="blockID" /> to <paramref name="data" /> using the block size specified by the flashconfig
        /// <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="blockID"/> is greater then total blocks on device</exception>
        /// </summary>
        /// <param name="blockID"> </param>
        /// <param name="data"> </param>
        /// <param name="verboseLevel"> </param>
        public void ReadBlock(uint blockID, out byte[] data, int verboseLevel = 0) {
            CheckFlashState();
            if (blockID > _xcfg.SizeSmallBlocks)
                throw new ArgumentOutOfRangeException("blockID");
            SendCMD(Commands.DataRead, blockID, _xcfg.BlockRawSize);
            data = new byte[_xcfg.BlockSize];
            var err = ReadFromDevice(data);
            if(err != ErrorCode.Success)
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            var status = GetFlashStatus(blockID);
            IsBadBlock(status, blockID, "Reading", verboseLevel >= 1);
        }

        public void Read(uint startBlock, uint blockCount, out byte[] data, int verboseLevel = 0) {
            CheckDeviceState();
            var sw = Stopwatch.StartNew();
            blockCount = _xcfg.FixBlockCount(startBlock, blockCount);
            var last = startBlock + blockCount;
            UpdateStatus(string.Format("Reading blocks 0x{0:X} -> 0x{1:X}", startBlock, last));
            var datalist = new List<byte>();
            for (var block = startBlock; block < last; block++)
            {
                if (_abort)
                {
                    sw.Stop();
                    UpdateStatus(string.Format("Erase aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                UpdateProgress(block, last);
                byte[] tmp;
                ReadBlock(block, out tmp, verboseLevel);
                datalist.AddRange(tmp);
            }
            if (!_abort)
            {
                sw.Stop();
                UpdateStatus(string.Format("Erase completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }
            _abort = false;
            data = datalist.ToArray();
        }

        public void Read(uint startBlock, uint blockCount, string file, int verboseLevel = 0) {
            CheckDeviceState();
            var sw = Stopwatch.StartNew();
            blockCount = _xcfg.FixBlockCount(startBlock, blockCount);
            var last = startBlock + blockCount;
            var bw = OpenWriter(file);
            if (bw == null)
                throw new OperationCanceledException(string.Format("Unable to open {0} for write... Aborted by user!", file));
            UpdateStatus(string.Format("Reading blocks 0x{0:X} -> 0x{1:X}", startBlock, last));
            for (var block = startBlock; block < last; block++)
            {
                if (_abort)
                {
                    sw.Stop();
                    UpdateStatus(string.Format("Erase aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                UpdateProgress(block, last);
                byte[] data;
                ReadBlock(block, out data, verboseLevel);
                bw.Write(data);
            }
            if (!_abort)
            {
                sw.Stop();
                UpdateStatus(string.Format("Erase completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }
            _abort = false;
        }

        public void Read(uint startBlock, uint blockCount, IEnumerable<string> files, int verboseLevel = 0) {
            CheckDeviceState();
            throw new NotImplementedException();
        }

        #endregion Implementation of IFlasher

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