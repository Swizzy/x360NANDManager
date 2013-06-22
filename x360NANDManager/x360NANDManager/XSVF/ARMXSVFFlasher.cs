namespace x360NANDManager.XSVF {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using LibUsbDotNet.Main;
    using x360NANDManager.SPI;

    internal sealed class ARMXSVFFlasher : ARMFlasher, IXSVFFlasher {
        public ARMXSVFFlasher(int vendorID, int productID) : base(vendorID, productID) {
        }
        
        /// <summary>
        /// Initalize XSVF Mode
        /// </summary>
        private void InitXSVFMode() {
            CheckDeviceState();
            GetARMVersion();
            GetARMVersion();
        }

        /// <summary>
        /// Compress XSVF data
        /// </summary>
        /// <param name="data">XSVF Data to compress</param>
        /// <returns>Compressed data</returns>
        private byte[] CompressXSVF(IList<byte> data) {
            var rs = 0;
            var ret = new List<byte>();
            while(rs < data.Count) {
                if(data[rs] == data[rs + 1]) {
                    var re = rs;
                    while(re < data.Count && data[rs] == data[re])
                        re++;
                    var rl = re - rs;
                    ret.Add(data[rs]);
                    ret.Add(data[rs + 1]);
                    ret.Add((byte) (rl - 2));
                    rs += rl;
                }
                else {
                    ret.Add(data[rs]);
                    rs++;
                }
            }
            UpdateStatus(string.Format("Compressed to 0x{0:X} Bytes OK", ret.Count));
            return ret.ToArray();
        }

        /// <summary>
        /// Reads and Compresses a XSVF file
        /// </summary>
        /// <param name="file">File to read data from</param>
        /// <returns>Compressed data</returns>
        private byte[] CompressXSVF(string file) {
            var data = File.ReadAllBytes(file);
            UpdateStatus(string.Format("Read 0x{0:X} Bytes OK", data.Length));
            data = CompressXSVF(data);
            return data;
        }

        /// <summary>
        /// Sends the XSVF Execute command
        /// </summary>
        private void ExecuteXSVF() {
            SendCMD(Commands.XSVFExec);
            Thread.Sleep(2000);
            SendCMD(Commands.DataStatus, 0, 0x4);
        }

        /// <summary>
        /// Checks if the ARM Version is valid (Used for ARM only)
        /// </summary>
        /// <returns>True if ARM is version 3 or higher</returns>
        internal bool IsCompatibleVersion() {
            GetARMVersion();
            return ArmVersion >= 3;
        }

        #region Implementation of IXSVFFlasher

        /// <summary>
        /// Writes <paramref name="file"/> to a XC2C64A chip
        /// </summary>
        /// <param name="file">XSVF file to write</param>
        public void WriteXSVF(string file) {
            if(string.IsNullOrEmpty(file))
                throw new ArgumentNullException(file);
            if(!File.Exists(file))
                throw new FileNotFoundException(string.Format("{0} Don't exist!", file));
            InitXSVFMode();
            var data = CompressXSVF(file);
            SendCMD(Commands.DataWrite, 0, (uint)data.Length);
            var err = WriteToDevice(data);
            if(err != ErrorCode.None)
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            UpdateStatus(string.Format("0x{0:X} bytes sent OK!{1}Executing File...", data.Length, Environment.NewLine));
            ExecuteXSVF();
            UpdateStatus(GetARMStatus() != 0 ? "FAILED!" : "OK!");
        }

        /// <summary>
        /// Writes <paramref name="data"/> to a XC2C64A chip
        /// </summary>
        /// <param name="data">XSVF data to write</param>
        public void WriteXSVF(byte[] data) {
            if (data == null)
                throw new ArgumentNullException("data");
            InitXSVFMode();
            data = CompressXSVF(data);
            SendCMD(Commands.DataWrite, 0, (uint)data.Length);
            var err = WriteToDevice(data);
            if(err != ErrorCode.None)
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            UpdateStatus(string.Format("0x{0:X} bytes sent OK!{1}Executing File...", data.Length, Environment.NewLine));
            ExecuteXSVF();
            UpdateStatus(GetARMStatus() != 0 ? "FAILED!" : "OK!");
        }

        #endregion
    }
}