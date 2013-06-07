namespace x360NANDManager {
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using LibUsbDotNet.Main;

    public sealed class DeviceError : Exception {
        #region ErrorLevels enum

        /// <summary>
        ///   Error levels defined within the app
        /// </summary>
        public enum ErrorLevels {
            /// <summary>
            ///   Default value
            /// </summary>
            None = 0,

            /// <summary>
            ///   No Error (In itself this should probably be an error!)
            /// </summary>
            Success = None,

            /// <summary>
            ///   A Win32 Error occured
            /// </summary>
            Win32Error = 1,

            /// <summary>
            ///   Device not initalized (SPI ERROR)
            /// </summary>
            DeviceNotInitialized,

            /// <summary>
            ///   Device has crashed (SPI ERROR)
            /// </summary>
            DeviceCrashed,

            /// <summary>
            ///   No such device found (SPI ERROR)
            /// </summary>
            NoDeviceFound,

            /// <summary>
            ///   LibUSBDotNET ERROR (SPI ERROR)
            /// </summary>
            USBError,

            /// <summary>
            ///   Flash not Initalized (SPI ERROR)
            /// </summary>
            FlashNotInitialized,

            /// <summary>
            ///   Reset failed
            /// </summary>
            ResetFailed,

            /// <summary>
            ///   Unable to lock device for exclusive access (MMC ERROR)
            /// </summary>
            DeviceLockFailed,

            /// <summary>
            ///   Unable to unlock device from exclusive access(MMC ERROR)
            /// </summary>
            DeviceUnLockFailed,

            /// <summary>
            ///   Device is not compatible with the requested feature
            /// </summary>
            IncompatibleDevice
        }

        #endregion

        /// <summary>
        ///   Initalizes the Device Error exception
        /// </summary>
        /// <param name="errorLevel"> Error level within the app (If Win32Error we'll get it from the system using Marshal.GetLastWin32Error()) </param>
        /// <param name="usberror"> USB error by LibUSBDotNET (only for LibUSBDotNET errors) </param>
        /// <param name="message"> Message to send to the user </param>
        internal DeviceError(ErrorLevels errorLevel, ErrorCode usberror = ErrorCode.None, string message = "") {
            Message = message;
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

        /// <summary>
        ///   Error level
        /// </summary>
        /// <returns>The Error Level for this exception</returns>
        public ErrorLevels ErrorLevel { get; private set; }

        /// <summary>
        ///   Win32 Error code
        /// </summary>
        /// <returns>The Win32 Error Code</returns>
        public int Win32ErrorNumber { get; private set; }

        /// <summary>
        ///   LibUSBDotNET USB Error
        /// </summary>
        /// <returns>The LibUsbDotNet ErrorCode</returns>
        public ErrorCode USBError { get; private set; }

        /// <summary>
        ///   Gets the Win32 Error message
        /// </summary>
        /// <returns>The message that explains the meaning of the Win32 Error</returns>
        public string Win32ErrorString {
            get { return new Win32Exception(Win32ErrorNumber).Message; }
        }

        /// <summary>
        ///   Gets a message that describes the current exception.
        /// </summary>
        /// <returns> The error message that explains the reason for the exception, or an empty string(""). </returns>
        public new string Message { get; private set; }

        /// <summary>
        ///   Convert Error to string
        /// </summary>
        /// <returns> Error information </returns>
        public override string ToString() {
            var trace = new StackTrace(this);
            switch(ErrorLevel) {
                case ErrorLevels.Win32Error:
                    return string.Format("{0} Win32Error: {1} ({2}) {3}{4}{5}", ErrorLevel, Win32ErrorNumber, Win32ErrorString, Message, Environment.NewLine, trace);
                case ErrorLevels.USBError:
                    return string.Format("{0} USBError: {1} {2}{3}{4}", ErrorLevel, USBError, Message, Environment.NewLine, trace);
                default:
                    return string.Format("{0} {1}{2}{3}", ErrorLevel, Message, Environment.NewLine, trace);
            }
        }
    }
}