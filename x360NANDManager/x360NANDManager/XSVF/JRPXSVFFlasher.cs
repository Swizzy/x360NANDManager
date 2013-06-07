namespace x360NANDManager.XSVF {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using LibUsbDotNet.Main;
    using x360NANDManager.SPI;

    internal sealed class JRPXSVFFlasher : ARMFlasher, IXSVFFlasher
    {
        public JRPXSVFFlasher(int vendorID, int productID) : base(vendorID, productID) {
        }
        
        private void SendCMD(byte cmd, byte type, IList<byte> buf) {
            CheckDeviceState();
            Main.SendDebug(buf.Count == 1 ? String.Format("Sending CMD: 0x{0:X02} 0x{1:X02}", cmd, buf[0]) : String.Format("Sending CMD: 0x{0:X02}", cmd));
            var packet = new UsbSetupPacket(type, cmd, 0, 0, 0);
            int sent;
            Device.ControlTransfer(ref packet, buf, buf.Count, out sent);
        }

        private void SendCMD(byte cmd, byte type, short buflen, out byte[] buf)
        {
            CheckDeviceState();
            Main.SendDebug(String.Format("Sending CMD: 0x{0:X}", cmd));
            var packet = new UsbSetupPacket(type, cmd, 0, 0, buflen);
            buf = new byte[buflen];
            var gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
            int sent;
            Device.ControlTransfer(ref packet, gch.AddrOfPinnedObject(), buf.Length, out sent);
            gch.Free();
        }

        private void WaitForStatus(short waitFor, int waitTime = 1, int seconds = 100) {
// ReSharper disable RedundantAssignment
            var maxSeconds = seconds;
// ReSharper restore RedundantAssignment
            short status = 0;
            while (status != waitFor && seconds > 0)
            {
                System.Threading.Thread.Sleep(waitTime);
                byte[] ret;
                SendCMD(0x2F, 0xC0, 2, out ret);
                status = BitConverter.ToInt16(ret, 0);
                seconds--;
                Main.SendDebug(string.Format("Status: 0x{0:X} Cycle: {1}", status, Math.Abs(seconds - maxSeconds)));
            }
            if (seconds <= 0)
                throw new TimeoutException();
        }

        /// <summary>
        /// Initalize XSVF Mode
        /// </summary>
        private void InitXSVF() {
            CheckDeviceState();
            SendCMD(0x2E, 0x40, new byte[] { 0x20 });
            WaitForStatus(0x21, 100);
            var data = new byte[] { 0x07, 0x20, 0x12, 0x00, 0x12, 0x01, 0x02, 0x08, 0x01, 0x08, 0x00, 0x00, 0x00, 0x20, 0x01, 0x0f, 0xff, 0x8f, 0xff, 0x09, 0x00, 0x00, 0x00, 0x00, 0xf6, 0xe5, 0xf0, 0x93, 0x00, 0x00, 0x00, 0x00 };
            var err = WriteToDevice(data);
            if (err != ErrorCode.Success)
                throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
            SendCMD(0x2E, 0x40, new byte[] { 0x24});
            WaitForStatus(0x22, 100);
            SendCMD(0x2E, 0x40, new byte[] { 0x20 });
            WaitForStatus(0x21, 100);
        }

        #region Implementation of IXSVFFlasher

        /// <summary>
        /// Writes <paramref name="file"/> to a XC2C64A chip
        /// </summary>
        /// <param name="file">XSVF file to write</param>
        public void WriteXSVF(string file) {
            CheckDeviceState();
            UpdateStatus(string.Format("JR-Programmer XSVF Flasher started for:{1}{0}", file, Environment.NewLine));
            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException(file);
            if (!File.Exists(file))
                throw new FileNotFoundException(string.Format("{0} Don't exist!", file));
            var data = File.ReadAllBytes(file);
            UpdateStatus(string.Format("Successfully read 0x{0:X} bytes from:{2}{1}", data.Length, file, Environment.NewLine));
            WriteXSVF(data);
        }

        /// <summary>
        /// Writes <paramref name="data"/> to a XC2C64A chip
        /// </summary>
        /// <param name="data">XSVF data to write</param>
        public void WriteXSVF(byte[] data) {
            CheckDeviceState();
            if (data == null)
                throw new ArgumentNullException("data");
            AbortRequested = false;
            var sw = Stopwatch.StartNew();
            InitXSVF();
            UpdateStatus("XSVF Mode Initalized");
            var packetSize = 0;
            for (var i = 0; i < DeviceConfigInfo.InterfaceInfoList[0].EndpointInfoList.Count; i++) {
                if (DeviceConfigInfo.InterfaceInfoList[0].EndpointInfoList[i].Descriptor.EndpointID == 0x05)
                    packetSize = DeviceConfigInfo.InterfaceInfoList[0].EndpointInfoList[i].Descriptor.MaxPacketSize;
            }
            if (packetSize == 0)
                throw new Exception("Something went wrong with grabbing the packet size!");
            for (var i = 0; i < data.Length; i += packetSize)
            {
                if (AbortRequested)
                {
                    sw.Stop();
                    UpdateStatus(string.Format("XSVF Flashing aborted after {0:F0} Minutes {1:F0} Seconds", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
                    Release();
                    return;
                }
                UpdateProgress((uint)(i / packetSize), (uint)(data.Length / packetSize));
                var tmp = new byte[packetSize];
                Buffer.BlockCopy(data, i, tmp, 0, ((i + packetSize >= data.Length) ? data.Length % packetSize : packetSize));
                var err = WriteToDevice(tmp);
                if (err != ErrorCode.Success)
                    throw new DeviceError(DeviceError.ErrorLevels.USBError, err);
                WaitForStatus((short)((i + packetSize >= data.Length) ? 0x22 : 0x21));
            }
            Release();
            UpdateStatus(string.Format("XSVF Flashing completed after {0:F0} Minutes {1:F0} Seconds", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds));
        }
        #endregion
    }
}