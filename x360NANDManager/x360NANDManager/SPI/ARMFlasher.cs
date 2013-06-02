namespace x360NANDManager.SPI {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using LibUsbDotNet;
    using LibUsbDotNet.Main;

    internal sealed class ARMFlasher : FlasherOutput, ISPIFlasher {
        #region Private Data Properties

        private bool _abort;
        private UsbDevice _device;
        private bool _flashInitialized;
        private int _productID;
        private UsbEndpointReader _reader;
        private int _vendorID;
        private UsbEndpointWriter _writer;
        private XConfig _xcfg;

        #endregion Private Data Properties

        public ARMFlasher() {
            if(DeviceInit(0xFFFF, 0x4) || DeviceInit(0x11D4, 0x8338))
                return;
            Release();
            throw new DeviceError(DeviceError.ErrorLevels.NoDeviceFound);
        }

        public uint ArmVersion { get; private set; }

        ~ARMFlasher() {
            Release();
        }

        #region Private methods

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

        private bool DeviceInit(int vendorID, int productID) {
            try {
                _vendorID = vendorID;
                _productID = productID;
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
                _reader = _device.OpenEndpointReader((ReadEndpointID) 0x82);
                _reader.ReadFlush();
                _writer = _device.OpenEndpointWriter((WriteEndpointID) 0x05);
                UsbDevice.UsbErrorEvent += UsbDeviceOnUsbErrorEvent;
                return true;
            }
            catch(Exception ex) {
                Main.SendDebug(String.Format("Device Init exception occured: {0}", ex.Message));
                throw;
            }
        }

        private void DeviceReset() {
            var wholeUsbDevice = _device as IUsbDevice;
            if(ReferenceEquals(wholeUsbDevice, null))
                return;
            wholeUsbDevice.ReleaseInterface(0);
            wholeUsbDevice.ResetDevice();
            Main.SendDebug("Device Successfully reset!");
        }

        private ErrorCode ReadFromDevice(ref byte[] buf, int tries = 10) {
            var totalread = 0;
            var err = ErrorCode.None;
            Main.SendDebug(string.Format("Reading 0x{0:X} Bytes from device", buf.Length));
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
                Main.SendDebug(string.Format("Read 0x{0:X} Attempt: {1} Total read data: 0x{2:X}", read, Math.Abs(tries - 10), totalread));
            }
            return tries > 0 ? ErrorCode.Success : err;
        }

        private ErrorCode WriteToDevice(byte[] buf, int tries = 10) {
            if(buf == null)
                throw new ArgumentNullException("buf");
            Main.SendDebug(string.Format("Writing 0x{0:X} Bytes to device", buf.Length));
            var totalwrote = 0;
            var err = ErrorCode.None;
            while(totalwrote < buf.Length && tries > 0) {
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
                Main.SendDebug(string.Format("Wrote 0x{0:X} Attempt: {1} Total written data: 0x{2:X}", wrote, Math.Abs(tries - 10), totalwrote));
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
            var err = ReadFromDevice(ref buf);
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

        #endregion

        #region Implementation of IFlasher

        /// <summary>
        ///   Initialize the flash before operations start
        ///   <exception cref="DeviceError">If Device is not initalized, there is a fatal USB error or for unsupported flashconfig types</exception>
        ///   <exception cref="ArgumentException">If flashconfig is invalid</exception>
        /// </summary>
        /// <param name="config"> Flashconfig information (Information about the consoles memory) </param>
        public void Init(out XConfig config) {
            CheckDeviceState();
            SendCMD(Commands.DevVersion, 0, 4);
            ArmVersion = ReadUInt32();
            UpdateStatus(string.Format("Arm Version: {0}", ArmVersion));
            Main.SendDebug(string.Format("Arm Version: {0}", ArmVersion));
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
            }
            UsbDevice.UsbErrorEvent -= UsbDeviceOnUsbErrorEvent;
            UsbDevice.Exit();
        }

        /// <summary>
        ///   Cycle device between operations
        ///   <exception cref="DeviceError">If there is any problem with the device or the reset fails</exception>
        /// </summary>
        public void Reset() {
            DeInit();
            Release();
            if (DeviceInit(_vendorID, _productID)) {
                XConfig tmp;
                Init(out tmp);
                if(_xcfg.Config == tmp.Config)
                    return;
            }
            throw new DeviceError(DeviceError.ErrorLevels.ResetFailed);
        }

        /// <summary>
        ///   Abort Operation
        /// </summary>
        public void Abort() {
            _abort = true;
        }

        /// <summary>
        ///   Sends the erase command for <paramref name="blockID" />
        ///   <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        ///   <exception cref="ArgumentOutOfRangeException">If
        ///     <paramref name="blockID" />
        ///     is greater then total blocks on device</exception>
        /// </summary>
        /// <param name="blockID"> Block ID to erase </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on erase errors or just a erase error (default = only print write error without details) </param>
        public void EraseBlock(uint blockID, int verboseLevel = 0) {
            CheckFlashState();
            if(blockID > _xcfg.SizeSmallBlocks)
                throw new ArgumentOutOfRangeException("blockID");
            SendCMD(Commands.DataErase, blockID, 0x4);
            _reader.ReadFlush();
            var status = GetFlashStatus(blockID);
            IsBadBlock(status, blockID, "Erasing", verboseLevel >= 1);
        }

        /// <summary>
        ///   Sends the erase command for BlockID: <paramref name="startBlock" /> and onwards for <paramref name="blockCount" />
        ///   <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        ///   <exception cref="ArgumentException">If there is a problem with your block count settings</exception>
        /// </summary>
        /// <param name="startBlock"> Starting blockID </param>
        /// <param name="blockCount"> Block count (Small blocks!) if set to 0 full device erase will be done </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on erase errors or just a erase error (default = only print write error without details) </param>
        public void Erase(uint startBlock, uint blockCount, int verboseLevel = 0) {
            CheckDeviceState();
            _abort = false;
            var sw = Stopwatch.StartNew();
            XConfig xConfig;
            Init(out xConfig);
            PrintXConfig(xConfig, verboseLevel);
            blockCount = _xcfg.FixBlockCount(startBlock, blockCount);
            var last = startBlock + blockCount;
            UpdateStatus(string.Format("Erasing blocks 0x{0:X} -> 0x{1:X}", startBlock, last));
            for(var block = startBlock; block < last; block ++) {
                if(_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Erase aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                UpdateProgress(block, last);
                EraseBlock(block, verboseLevel);
            }
            if(_abort)
                return;
            sw.Stop();
            UpdateStatus(string.Format("Erase completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            DeInit();
            Release();
        }

        /// <summary>
        ///   Writes the data of <paramref name="data" /> to <paramref name="blockID" /> <c>as is</c>
        ///   <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        ///   <exception cref="ArgumentOutOfRangeException">If
        ///     <paramref name="blockID" />
        ///     is greater then total blocks on device</exception>
        /// </summary>
        /// <param name="blockID"> Block ID to write to </param>
        /// <param name="data"> Data to write </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on write errors or just a write error (default = only print write error without details) </param>
        public void WriteBlock(uint blockID, byte[] data, int verboseLevel = 0) {
            CheckFlashState();
            if (data.Length != 0x4200)
                throw new ArgumentException(string.Format("Data must be 0x{0:X} bytes in length for this flashconfig!", 0x4200));
            if(blockID > _xcfg.SizeSmallBlocks)
                throw new ArgumentOutOfRangeException("blockID");
            SendCMD(Commands.DataWrite, blockID, (uint) data.Length);
            var err = WriteToDevice(data);
            if(err != ErrorCode.Success)
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            if (_vendorID != 0x11D4 && _productID != 0x8338)
                SendCMD(Commands.DataExec, blockID);
            var status = GetFlashStatus(blockID);
            IsBadBlock(status, blockID, "Writing", verboseLevel >= 1);
        }

        /// <summary>
        ///   Writes buffer to device using specified write mode (<paramref name="mode" />) starting at <paramref name="startBlock" /> and writing untill the end of the the buffer or <paramref
        ///    name="blockCount" />
        /// </summary>
        /// <param name="startBlock"> Starting blockID </param>
        /// <param name="blockCount"> Block count (Small blocks!) if set to 0 full device/file write will be done </param>
        /// <param name="data"> Data Buffer to write </param>
        /// <param name="mode"> Write Mode to use (Default = None/RAW [Write data as is]) </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on write errors or just a write error (default = only print write error without details) </param>
        public void Write(uint startBlock, uint blockCount, byte[] data, SPIWriteModes mode = SPIWriteModes.None, int verboseLevel = 0) {
            CheckDeviceState();
            _abort = false;
            XConfig xConfig;
            Init(out xConfig);
            PrintXConfig(xConfig, verboseLevel);
            var addSpare = (mode & SPIWriteModes.AddSpare) == SPIWriteModes.AddSpare;
            var correctSpare = (mode & SPIWriteModes.CorrectSpare) == SPIWriteModes.CorrectSpare;
            var eraseFirst = (mode & SPIWriteModes.EraseFirst) == SPIWriteModes.EraseFirst;
            var verify = (mode & SPIWriteModes.VerifyAfter) == SPIWriteModes.VerifyAfter;
            var dataList = new List<byte>();
            Stopwatch sw;

            #region Preperations

            if (addSpare)
            {
                var datablocks = xConfig.SizeToBlocks(data.Length);
                if (datablocks < blockCount || blockCount == 0)
                    blockCount = datablocks;
            }
            else
            {
                var datablocks = xConfig.SizeToRawBlocks(data.Length);
                if (datablocks < blockCount || blockCount == 0)
                    blockCount = datablocks;
            }
            blockCount = xConfig.FixBlockCount(startBlock, blockCount);
            var lastBlock = startBlock + blockCount - 1;
            var totalBlocks = lastBlock;
            if (eraseFirst)
                totalBlocks += lastBlock;
            if (verify)
                totalBlocks += lastBlock;

            UpdateStatus("Starting write with the following settings:");
            UpdateStatus(string.Format("Erase before writing: {0}", eraseFirst ? "Enabled" : "Disabled"));
            UpdateStatus(string.Format("Verify after writing: {0}", verify ? "Enabled" : "Disabled"));
            UpdateStatus(string.Format("Write Mode: {0}", addSpare ? "Add Spare" : correctSpare ? "Correct Spare" : "RAW"));
            UpdateStatus(string.Format("Starting block: 0x{0:X} Last Block: 0x{1:X}", startBlock, lastBlock));

            #endregion Preperations

            #region Erase First

            if (eraseFirst) {
                sw = Stopwatch.StartNew();
                UpdateStatus(string.Format("Erasing blocks 0x{0:X} -> 0x{1:X}", startBlock, lastBlock));
                for (var block = startBlock; block <= lastBlock; block++) {
                    if (_abort)
                    {
                        sw.Stop();
                        UpdateStatus(string.Format("Erase aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                        break;
                    }
                    UpdateProgress(block, lastBlock, totalBlocks);
                    EraseBlock(block, verboseLevel);
                }
                sw.Stop();
                UpdateStatus(string.Format("Erase Completed after {0:F0} Minutes and {1:F0} Seconds!\nDevice will be reset before writing...", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                Reset();
            }

            #endregion Erase First

            #region Write

            int offset;
            if (!_abort) {
                offset = 0;
                sw = Stopwatch.StartNew();
                UpdateStatus(string.Format("Writing blocks 0x{0:X} -> 0x{1:X}", startBlock, lastBlock));
                for (var block = startBlock; block <= lastBlock; block++)
                {
                    if (_abort)
                    {
                        sw.Stop();
                        UpdateStatus(string.Format("Write aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                        break;
                    }
                    UpdateProgress(block + (eraseFirst ? lastBlock : 0), lastBlock, totalBlocks);
                    var tmp = addSpare ? new byte[0x4000] : new byte[0x4200];
                    Buffer.BlockCopy(data, offset, tmp, 0, tmp.Length);
                    offset += tmp.Length;
                    if (addSpare)
                        AddSpareBlock(ref tmp, block, xConfig.MetaType);
                    else if (correctSpare)
                        CorrectSpareBlock(ref tmp, block, xConfig.MetaType);
                    WriteBlock(block, tmp, verboseLevel);
                    if (verify)
                        dataList.AddRange(tmp);
                }
                sw.Stop();
                UpdateStatus(string.Format("Write Completed after {0:F0} Minutes and {1:F0} Seconds!\nDevice will be reset before verifying...", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }

            #endregion Write

            #region Verify

            if(!_abort && verify) {
                Reset();
                sw = Stopwatch.StartNew();
                UpdateStatus(string.Format("Verifying blocks 0x{0:X} -> 0x{1:X}", startBlock, lastBlock));
                offset = 0;
                var writtenData = dataList.ToArray();
                dataList.Clear();
                for(var block = startBlock; block <= lastBlock; block++) {
                    if(_abort) {
                        sw.Stop();
                        UpdateStatus(string.Format("Verify aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                        break;
                    }
                    UpdateProgress(block + totalBlocks - lastBlock, lastBlock, totalBlocks);
                    byte[] tmp;
                    ReadBlock(block, out tmp, verboseLevel);
                    if(!CompareByteArrays(tmp, writtenData, offset))
                        SendError(string.Format("Verification of block 0x{0:X} Failed!", block));
                    offset += tmp.Length;
                }
                sw.Stop();
                UpdateStatus(string.Format("Verify Completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }
            #endregion

            DeInit();
            Release();
        }

        /// <summary>
        ///   Writes file to device using specified write mode (<paramref name="mode" />) starting at <paramref name="startBlock" /> and writing untill the end of the file or <paramref
        ///    name="blockCount" />
        /// </summary>
        /// <param name="startBlock"> Starting blockID </param>
        /// <param name="blockCount"> Block count (Small blocks!) if set to 0 full device/file write will be done </param>
        /// <param name="file"> File to write </param>
        /// <param name="mode"> Write Mode to use (Default = None/RAW [Write data as is]) </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on write errors or just a write error (default = only print write error without details) </param>
        public void Write(uint startBlock, uint blockCount, string file, SPIWriteModes mode = SPIWriteModes.None, int verboseLevel = 0) {
            CheckDeviceState();
            _abort = false;
            XConfig xConfig;
            Init(out xConfig);
            PrintXConfig(xConfig, verboseLevel);
            var addSpare = (mode & SPIWriteModes.AddSpare) == SPIWriteModes.AddSpare;
            var correctSpare = (mode & SPIWriteModes.CorrectSpare) == SPIWriteModes.CorrectSpare;
            var eraseFirst = (mode & SPIWriteModes.EraseFirst) == SPIWriteModes.EraseFirst;
            var verify = (mode & SPIWriteModes.VerifyAfter) == SPIWriteModes.VerifyAfter;
            var dataList = new List<byte>();
            Stopwatch sw;
            var br = OpenReader(file);
            if(br == null)
                throw new OperationCanceledException(string.Format("Unable to open {0} for reading... Aborted by user!", file));

            #region Preperations

            var datablocks = xConfig.GetFileBlockCount(file);
            if (datablocks < blockCount || blockCount == 0)
                blockCount = datablocks;
            var lastBlock = startBlock + blockCount - 1;
            var totalBlocks = lastBlock;
            if (eraseFirst)
                totalBlocks += lastBlock;
            if (verify)
                totalBlocks += lastBlock;

            UpdateStatus("Starting write with the following settings:");
            UpdateStatus(string.Format("Erase before writing: {0}", eraseFirst ? "Enabled" : "Disabled"));
            UpdateStatus(string.Format("Verify after writing: {0}", verify ? "Enabled" : "Disabled"));
            UpdateStatus(string.Format("Write Mode: {0}", addSpare ? "Add Spare" : correctSpare ? "Correct Spare" : "RAW"));
            UpdateStatus(string.Format("Starting block: 0x{0:X} Last Block: 0x{1:X}", startBlock, lastBlock));

            #endregion Preperations

            #region Erase First

            if (eraseFirst)
            {
                sw = Stopwatch.StartNew();
                UpdateStatus(string.Format("Erasing blocks 0x{0:X} -> 0x{1:X}", startBlock, lastBlock));
                for(var block = startBlock; block <= lastBlock; block++)
                {
                    if (_abort)
                    {
                        sw.Stop();
                        UpdateStatus(string.Format("Erase aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                        break;
                    }
                    UpdateProgress(block, lastBlock, totalBlocks);
                    EraseBlock(block, verboseLevel);
                }
                sw.Stop();
                UpdateStatus(string.Format("Erase Completed after {0:F0} Minutes and {1:F0} Seconds!\nDevice will be reset before writing...", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                Reset();
            }

            #endregion Erase First

            #region Write

            if (!_abort)
            {
                sw = Stopwatch.StartNew();
                UpdateStatus(string.Format("Writing blocks 0x{0:X} -> 0x{1:X}", startBlock, lastBlock));
                for(var block = startBlock; block <= lastBlock; block++)
                {
                    if (_abort)
                    {
                        sw.Stop();
                        UpdateStatus(string.Format("Write aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                        break;
                    }
                    UpdateProgress(block + (eraseFirst ? lastBlock : 0), lastBlock, totalBlocks);
                    var tmp = br.ReadBytes(addSpare ? 0x4000 : 0x4200);
                    if (addSpare)
                        AddSpareBlock(ref tmp, block, xConfig.MetaType);
                    else if (correctSpare)
                        CorrectSpareBlock(ref tmp, block, xConfig.MetaType);
                    WriteBlock(block, tmp, verboseLevel);
                    if (verify)
                        dataList.AddRange(tmp);
                }
                sw.Stop();
                UpdateStatus(string.Format("Write Completed after {0:F0} Minutes and {1:F0} Seconds!\nDevice will be reset before verifying...", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }

            #endregion Write

            #region Verify

            if (!_abort && verify)
            {
                Reset();
                sw = Stopwatch.StartNew();
                UpdateStatus(string.Format("Verifying blocks 0x{0:X} -> 0x{1:X}", startBlock, lastBlock));
                var offset = 0;
                var writtenData = dataList.ToArray();
                dataList.Clear();
                for(var block = startBlock; block <= lastBlock; block++)
                {
                    if (_abort)
                    {
                        sw.Stop();
                        UpdateStatus(string.Format("Verify aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                        break;
                    }
                    UpdateProgress(block + totalBlocks - lastBlock, lastBlock, totalBlocks);
                    byte[] tmp;
                    ReadBlock(block, out tmp, verboseLevel);
                    if (!CompareByteArrays(tmp, writtenData, offset))
                        SendError(string.Format("Verification of block 0x{0:X} Failed!", block));
                    offset += tmp.Length;
                }
                sw.Stop();
                UpdateStatus(string.Format("Verify Completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }
            #endregion Verify

            DeInit();
            Release();
        }

        /// <summary>
        ///   Reads <paramref name="blockID" /> to <paramref name="data" /> using the block size specified by the flashconfig
        ///   <exception cref="DeviceError">If Device is not initalized or there is a fatal USB error</exception>
        ///   <exception cref="ArgumentOutOfRangeException">If
        ///     <paramref name="blockID" />
        ///     is greater then total blocks on device</exception>
        /// </summary>
        /// <param name="blockID"> Block to Read </param>
        /// <param name="data"> Data from the nand </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on read errors or just a read error (default = only print write error without details) </param>
        public void ReadBlock(uint blockID, out byte[] data, int verboseLevel = 0) {
            CheckFlashState();
            if(blockID > _xcfg.SizeSmallBlocks)
                throw new ArgumentOutOfRangeException("blockID");
            data = new byte[_xcfg.BlockRawSize];
            SendCMD(Commands.DataRead, blockID, (uint) data.Length);
            var err = ReadFromDevice(ref data);
            if(err != ErrorCode.Success)
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            var status = GetFlashStatus(blockID);
            IsBadBlock(status, blockID, "Reading", verboseLevel >= 1);
        }

        /// <summary>
        ///   Reads data from nand from block <paramref name="startBlock" /> and onwards for <paramref name="blockCount" /> to <paramref
        ///    name="data" />
        /// </summary>
        /// <param name="startBlock"> Starting blockID </param>
        /// <param name="blockCount"> Block count (Small blocks!) if set to 0 full device dump will be made </param>
        /// <param name="data"> Data buffer from nand </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on read errors or just a read error (default = only print write error without details) </param>
        public void Read(uint startBlock, uint blockCount, out byte[] data, int verboseLevel = 0) {
            CheckDeviceState();
            _abort = false;
            var sw = Stopwatch.StartNew();
            XConfig xConfig;
            Init(out xConfig);
            PrintXConfig(xConfig, verboseLevel);
            blockCount = _xcfg.FixBlockCount(startBlock, blockCount);
            var last = startBlock + blockCount;
            UpdateStatus(string.Format("Reading blocks 0x{0:X} -> 0x{1:X}", startBlock, last));
            var datalist = new List<byte>();
            for(var block = startBlock; block < last; block++) {
                if(_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Erase aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                UpdateProgress(block, last);
                byte[] tmp;
                ReadBlock(block, out tmp, verboseLevel);
                datalist.AddRange(tmp);
            }
            DeInit();
            Release();
            if(!_abort) {
                sw.Stop();
                UpdateStatus(string.Format("Erase completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
            }
            data = datalist.ToArray();
        }

        /// <summary>
        ///   Reads data from nand from block <paramref name="startBlock" /> and onwards for <paramref name="blockCount" /> to <paramref
        ///    name="file" />
        /// </summary>
        /// <param name="startBlock"> Starting blockID </param>
        /// <param name="blockCount"> Block count (Small blocks!) if set to 0 full device dump will be made </param>
        /// <param name="file"> File to save data in </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on read errors or just a read error (default = only print write error without details) </param>
        public void Read(uint startBlock, uint blockCount, string file, int verboseLevel = 0) {
            CheckDeviceState();
            var sw = Stopwatch.StartNew();
            XConfig xConfig;
            Init(out xConfig);
            PrintXConfig(xConfig, verboseLevel);
            blockCount = _xcfg.FixBlockCount(startBlock, blockCount);
            var last = startBlock + blockCount;
            var bw = OpenWriter(file);
            if(bw == null)
                throw new OperationCanceledException(string.Format("Unable to open {0} for write... Aborted by user!", file));
            UpdateStatus(string.Format("Reading blocks 0x{0:X} -> 0x{1:X}", startBlock, last));
            for(var block = startBlock; block < last; block++) {
                if(_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Read aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                UpdateProgress(block, last);
                byte[] data;
                ReadBlock(block, out data, verboseLevel);
                bw.Write(data);
            }
            DeInit();
            Release();
            if(_abort)
                return;
            sw.Stop();
            UpdateStatus(string.Format("Read completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
        }

        /// <summary>
        ///   Reads data from nand from block <paramref name="startBlock" /> and onwards for <paramref name="blockCount" /> to <paramref
        ///    name="files" />
        /// </summary>
        /// <param name="startBlock"> Starting blockID </param>
        /// <param name="blockCount"> Block count (Small blocks!) if set to 0 full device dump will be made </param>
        /// <param name="files"> Files to save data in </param>
        /// <param name="verboseLevel"> Specifies if you want alot of information on read errors or just a read error (default = only print write error without details) </param>
        public void Read(uint startBlock, uint blockCount, List<string> files, int verboseLevel = 0) {
            CheckDeviceState();
            _abort = false;
            var sw = Stopwatch.StartNew();
            RemoveDuplicatesInList(ref files);
            foreach(var file in files) {
                if(_abort) {
                    sw.Stop();
                    UpdateStatus(string.Format("Read aborted after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    break;
                }
                Read(startBlock, blockCount, file, verboseLevel);
                Reset();
            }
            if(_abort)
                return;
            sw.Stop();
            UpdateStatus(string.Format("Read completed after {0:F0} Minutes and {1:F0} Seconds!", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
        }

        #endregion Verify

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