namespace x360NANDManager.XSVF {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using LibUsbDotNet.Main;
    using x360NANDManager.SPI;

    internal sealed class JRPXSVFFlasher : ARMFlasher, IXSVFFlasher {
        public JRPXSVFFlasher(int vendorID, int productID) : base(vendorID, productID) {
        }

        private void SendCMD(XSVFCommands cmd, byte type, IList<byte> buf) {
            CheckDeviceState();
            switch(buf.Count) {
                case 1:
                    Main.SendDebug("Sending CMD: {0} {1}", cmd, (XSVFCommands) buf[0]);
                    break;
                default:
                    Main.SendDebug("Sending CMD: {0}", cmd);
                    break;
            }
            var packet = new UsbSetupPacket(type, (byte) cmd, 0, 0, 0);
            int sent;
            Device.ControlTransfer(ref packet, buf, buf.Count, out sent);
        }

        private void SendCMD(XSVFCommands cmd, byte type, short buflen, out byte[] buf) {
            CheckDeviceState();
            Main.SendDebug("Sending CMD: {0}", cmd);
            var packet = new UsbSetupPacket(type, (byte) cmd, 0, 0, buflen);
            buf = new byte[buflen];
            var gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
            int sent;
            Device.ControlTransfer(ref packet, gch.AddrOfPinnedObject(), buf.Length, out sent);
            gch.Free();
        }

        private bool WaitForStatus(XSVFCommands waitFor, int waitTime = 1, int maxTries = 100, bool throwErr = true) {
            Main.SendDebug("Waiting for status {0}", waitFor);
            var tries = maxTries;
            XSVFCommands status = 0;
            while(status != waitFor && tries > 0) {
                Thread.Sleep(waitTime);
                byte[] ret;
                SendCMD(XSVFCommands.XSVFPoll, 0xC0, 2, out ret);
                status = (XSVFCommands) ret[0];
                tries--;
                Main.SendDebug("Status: {0} Cycle: {1}", status, Math.Abs(tries - maxTries));
                if(status != XSVFCommands.XSVFError || waitFor == XSVFCommands.XSVFError)
                    continue;
                if(!throwErr)
                    return false;
                throw new Exception(string.Format("XSVFError encountered! (0x{0:X2})", ret[1]));
            }
            if(tries <= 0)
                throw new TimeoutException();
            return true;
        }

        private bool SendInitBytes(ref byte[] data) {
            SendCMD(XSVFCommands.XSVFCmd, 0x40, new[] {
                                                      (byte) XSVFCommands.XSVFPlay
                                                      });
            WaitForStatus(XSVFCommands.XSVFOut, 100);
            var err = WriteToDevice(data);
            if(err != ErrorCode.Success)
                throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.USBError, err);
            return WaitForStatus(XSVFCommands.XSVFOk, 100, throwErr: false);
        }

        /// <summary>
        ///   Initalize XSVF Mode
        /// </summary>
        private void InitXSVF() {
            CheckDeviceState();
            var data = new byte[] {
                                  0x07, 0x20, 0x12, 0x00, 0x12, 0x01, 0x02, 0x08, 0x01, 0x08, 0x00, 0x00, 0x00, 0x20, 0x01, 0x0F, 0xFF, 0x8F, 0xFF, 0x09, 0x00, 0x00, 0x00, 0x00, 0xF6, 0xE5, 0xF0, 0x93, 0x00, 0x00, 0x00, 0x00 // XC2C64A
                                  };
            if(!SendInitBytes(ref data)) {
                data = new byte[] {
                                  0x07, 0x20, 0x12, 0x00, 0x12, 0x01, 0x02, 0x08, 0x01, 0x08, 0x00, 0x00, 0x00, 0x20, 0x01, 0x0F, 0xFF, 0x8F, 0xFF, 0x09, 0x00, 0x00, 0x00, 0x00, 0xF6, 0xE1, 0xF0, 0x93, 0x00, 0x00, 0x00, 0x00
                                  };
                if(!SendInitBytes(ref data)) {
                    data = new byte[] {
                                      0x07, 0x20, 0x12, 0x00, 0x12, 0x01, 0x02, 0x08, 0x01, 0x08, 0x00, 0x00, 0x00, 0x20, 0x01, 0x0F, 0xFF, 0x8F, 0xFF, 0x09, 0x00, 0x00, 0x00, 0x00, 0xF6, 0xD8, 0xF0, 0x93, 0x00, 0x00, 0x00, 0x00 // XC2C128-VQ100 (DGX)
                                      };
                    if(!SendInitBytes(ref data))
                        throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.IdentFailed);
                }
            }
            UpdateStatus("Erasing CPLD...");
            SendCMD(XSVFCommands.XSVFCmd, 0x40, new[] {
                                                      (byte) XSVFCommands.XSVFErase
                                                      });
            WaitForStatus(XSVFCommands.XSVFOk, 100);
            UpdateStatus("CPLD Erased OK!");
            UpdateStatus("Starting Interpreter...");
            SendCMD(XSVFCommands.XSVFCmd, 0x40, new[] {
                                                      (byte) XSVFCommands.XSVFPlay
                                                      });
            WaitForStatus(XSVFCommands.XSVFOut, 100);
            UpdateStatus("Interpreter started OK!");
        }

        #region Implementation of IXSVFFlasher

        /// <summary>
        ///   Writes <paramref name="file" /> to a XC2C64A chip
        /// </summary>
        /// <param name="file"> XSVF file to write </param>
        public void WriteXSVF(string file) {
            CheckDeviceState();
            UpdateStatus("JR-Programmer XSVF Flasher started for:{1}{0}", file, Environment.NewLine);
            if(string.IsNullOrEmpty(file))
                throw new ArgumentNullException(file);
            if(!File.Exists(file))
                throw new FileNotFoundException(string.Format("{0} Don't exist!", file));
            var data = File.ReadAllBytes(file);
            UpdateStatus("Successfully read 0x{0:X} bytes from:{2}{1}", data.Length, file, Environment.NewLine);
            WriteXSVF(data);
        }

        /// <summary>
        ///   Writes <paramref name="data" /> to a XC2C64A chip
        /// </summary>
        /// <param name="data"> XSVF data to write </param>
        public void WriteXSVF(byte[] data) {
            CheckDeviceState();
            if(data == null)
                throw new ArgumentNullException("data");
            AbortRequested = false;
            var sw = Stopwatch.StartNew();
            InitXSVF();
            var packetSize = 0;
            for(var i = 0; i < DeviceConfigInfo.InterfaceInfoList[0].EndpointInfoList.Count; i++) {
                if(DeviceConfigInfo.InterfaceInfoList[0].EndpointInfoList[i].Descriptor.EndpointID == 0x05)
                    packetSize = DeviceConfigInfo.InterfaceInfoList[0].EndpointInfoList[i].Descriptor.MaxPacketSize;
            }
            if(packetSize == 0)
                throw new Exception("Something went wrong with grabbing the packet size!");
            UpdateStatus("Sending out packets...");
            for(var i = 0; i < data.Length; i += packetSize) {
                if(AbortRequested) {
                    sw.Stop();
                    UpdateStatus("XSVF Flashing aborted after {0:F0} Minutes {1:F0} Seconds", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds);
                    Release();
                    return;
                }
                UpdateProgress((uint) i, (uint) data.Length);
                var tmp = new byte[packetSize];
                Buffer.BlockCopy(data, i, tmp, 0, (i + packetSize >= data.Length ? data.Length % packetSize : packetSize));
                var err = WriteToDevice(tmp);
                if(err != ErrorCode.Success)
                    throw new X360NANDManagerException(X360NANDManagerException.ErrorLevels.USBError, err);
                WaitForStatus(i + packetSize >= data.Length ? XSVFCommands.XSVFOk : XSVFCommands.XSVFOut, 2);
                if(i + packetSize >= data.Length)
                    UpdateProgress((uint) data.Length, (uint) data.Length);
            }
            UpdateStatus("All packets sent OK!");
            Release();
            UpdateStatus("XSVF Flashing completed after {0:F0} Minutes {1:F0} Seconds", sw.Elapsed.TotalMinutes, sw.Elapsed.Seconds);
        }

        #endregion

        #region Nested type: XSVFCommands

        private enum XSVFCommands : byte {
            XSVFPlay = 0x20,
            XSVFOut = 0x21,
            XSVFOk = 0x22,
            XSVFError = 0x23,
            XSVFErase = 0x24,
            XSVFCmd = 0x2E,
            XSVFPoll = 0x2F,
        }

        #endregion
    }
}