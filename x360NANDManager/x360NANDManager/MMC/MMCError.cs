namespace x360NANDManager.MMC {
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    internal sealed class MMCError {
        public MMCError(ErrorLevels errorLevel) {
            ErrorLevel = errorLevel;
            if(errorLevel == ErrorLevels.Win32Error)
                Win32ErrorNumber = Marshal.GetLastWin32Error();
        }

        public ErrorLevels ErrorLevel { get; private set; }

        public int Win32ErrorNumber { get; private set; }

        public string Win32ErrorString {
            get { return new Win32Exception(Win32ErrorNumber).Message; }
        }

        public override string ToString() {
            return ErrorLevel == ErrorLevels.Win32Error ? string.Format("{0} Win32Error: {1} ({2})", ErrorLevel, Win32ErrorNumber, Win32ErrorString) : string.Format("{0}", ErrorLevel);
        }

        #region Nested type: ErrorLevels

        internal enum ErrorLevels {
            None = 0,
            Success = None,
            Win32Error = 1
        }

        #endregion
    }
}