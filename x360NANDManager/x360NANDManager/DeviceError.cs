namespace x360NANDManager {
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using LibUsbDotNet.Main;

    internal sealed class DeviceError : Exception {
        public DeviceError(ErrorLevels errorLevel, ErrorCode usberror = ErrorCode.None) {
            ErrorLevel = errorLevel;
            switch(errorLevel) {
                case ErrorLevels.Win32Error:
                    Win32ErrorNumber = Marshal.GetLastWin32Error();
                    break;
                case ErrorLevels.USBError:
                    USBError = usberror;
                    break;
            }
        }

        public ErrorLevels ErrorLevel { get; private set; }

        public int Win32ErrorNumber { get; private set; }

        public ErrorCode USBError { get; private set; }

        public string Win32ErrorString {
            get { return new Win32Exception(Win32ErrorNumber).Message; }
        }

        public override string ToString() {
            var trace = new StackTrace(this);
            switch(ErrorLevel) {
                case ErrorLevels.Win32Error:
                    return string.Format("{0} Win32Error: {1} ({2}){3}{4}", ErrorLevel, Win32ErrorNumber, Win32ErrorString, Environment.NewLine, trace);
                case ErrorLevels.USBError:
                    return string.Format("{0} USBError: {1}{2}{3}", ErrorLevel, USBError, Environment.NewLine, trace);
                default:
                    return string.Format("{0}{1}{2}", ErrorLevel, Environment.NewLine, trace);
            }
        }

        #region Nested type: ErrorLevels

        internal enum ErrorLevels {
            None = 0,
            Success = None,
            Win32Error = 1,
            DeviceNotInitialized,
            DeviceCrashed,
            NoDeviceFound,
            USBError,
            FlashNotInitialized,
            ResetFailed
        }

        #endregion
    }
}