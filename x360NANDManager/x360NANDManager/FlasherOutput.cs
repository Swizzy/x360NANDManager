namespace x360NANDManager {
    using System;

    public abstract class FlasherOutput : Utils, IFlasherOutput {
        #region Implementation of IFlasherOutput
        /// <summary>
        /// Sends status messages
        /// </summary>
        public event EventHandler<EventArg<string>> Status;
        /// <summary>
        /// Sends error messages (BadBlock messages for example)
        /// </summary>
        public event EventHandler<EventArg<string>> Error;
        /// <summary>
        /// Reports the progress (Current Block/Offset, Maximum Block/Offset (target) and Percentage
        /// </summary>
        public event EventHandler<EventArg<ProgressData>> Progress;

        #endregion Implementation of IFlasherOutput

        internal void UpdateProgress(uint current, uint max, uint total = 0) {
            if(Progress == null)
                return;
            if (total == 0)
                total = max;
            Progress(null, new EventArg<ProgressData>(new ProgressData {Current = current, Maximum = max, Percentage = ((double)(current + 1) / total) * 100}));
        }

        internal void UpdateStatus(string message)
        {
            if(Status != null && message != null)
                Status(null, new EventArg<string>(message));
        }

        internal void SendError(string message)
        {
            if(Error != null && message != null)
                Error(null, new EventArg<string>(message));
            Main.SendDebug(message);
        }

        internal bool IsBadBlock(uint status, uint block, string operation, bool verbose = false) {
            if(status != 0x200 && status > 0) {
                SendError(string.Format("ERROR: 0x{0:X} {1} block 0x{2:X}", status, operation, block));
                if(verbose) {
                    SendError("This error Means:");
                    if((status & 0x800) == 0x800)
                        SendError("Illegal Logical Address");
                    if((status & 0x400) == 0x400)
                        SendError("NAND Not Write Protected");
                    if((status & 0x100) == 0x100)
                        SendError("Interrupt");
                    if((status & 0x80) == 0x80)
                        SendError("Address Alignment Error");
                    if((status & 0x40) == 0x40)
                        SendError("Bad Block Marker Detected");
                    if((status & 0x20) == 0x20)
                        SendError("Logical Replacement not found");
                    if((status & 0x10) == 0x10)
                        SendError("ECC Error Detected");
                    else if((status & 0x8) == 0x8)
                        SendError("ECC Error Detected");
                    else if((status & 0x4) == 0x4)
                        SendError("ECC Error Detected");
                    if((status & 0x2) == 0x2)
                        SendError("Write/Erase Error Detected");
                    if((status & 0x1) == 0x1)
                        SendError("SPI Is Busy");
                }
                return true;
            }
            if(status == 0)
                SendError(string.Format("Unkown Status code (0) Encounterd! while {0} block 0x{1:X}", operation, block));
            return false;
        }
    }
}