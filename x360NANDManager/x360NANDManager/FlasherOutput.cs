namespace x360NANDManager {
    using System;
    using System.Globalization;

    public abstract class FlasherOutput : BlockUtils, IFlasherOutput {
        #region IFlasherOutput Implementation

        /// <summary>
        ///   Sends status messages
        /// </summary>
        public event EventHandler<EventArg<string>> Status;

        /// <summary>
        ///   Sends error messages (BadBlock messages for example)
        /// </summary>
        public event EventHandler<EventArg<string>> Error;

        /// <summary>
        ///   Reports the progress (Current Block/Offset, Maximum Block/Offset (target) and Percentage
        /// </summary>
        public event EventHandler<EventArg<ProgressData>> Progress;

        /// <summary>
        /// Fires the <seealso cref="Progress"/> event with specified information
        /// </summary>
        /// <param name="current">Current block</param>
        /// <param name="max">Last Block</param>
        /// <param name="total"> Total Block (Used for calculating progress with multipile parts)</param>
        public void UpdateProgress(uint current, uint max, uint total = 0) {
            if(Progress == null)
                return;
            if(total == 0)
                total = max;
            Progress(null, new EventArg<ProgressData>(new ProgressData {
                                                                       Current = current, Maximum = max, Percentage = ((double) (current + 1) / total) * 100
                                                                       }));
        }

        public void UpdateStatus(string message) {
            if(Status != null && message != null)
                Status(null, new EventArg<string>(message));
        }

        public void SendError(string message) {
            if(Error != null && message != null)
                Error(null, new EventArg<string>(message));
            Main.SendDebug(message);
        }

        public bool IsBadBlock(uint status, uint block, string operation, bool verbose = false) {
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

        #endregion IFlasherOutput Implementation

        public void PrintXConfig(XConfig cfg, int verboselevel = 0)
        {
            if (verboselevel >= 0)
                UpdateStatus(string.Format("FlashConfig:         0x{0:X08}", cfg.Config));
            if (verboselevel >= 1)
            {
                UpdateStatus(string.Format("Page Size:           0x{0:X}", cfg.PageSize));
                UpdateStatus(string.Format("Meta Size:           0x{0:X}", cfg.MetaSize));
                UpdateStatus(string.Format("Meta Type:           0x{0:X}", cfg.MetaType));
                UpdateStatus(string.Format("Block Size (RAW):    0x{0:X}", cfg.BlockRawSize));
                UpdateStatus(string.Format("Block Size:          0x{0:X}", cfg.BlockSize));
            }
            if (verboselevel >= 2)
                UpdateStatus(string.Format("Pages Per Block:     {0}", cfg.PagesPerBlock));
            if (verboselevel >= 0)
                UpdateStatus(string.Format("Size Blocks:         0x{0:X}", cfg.SizeBlocks));
            if (verboselevel >= 2)
            {
                UpdateStatus(string.Format("Small BlocksCount:   0x{0:X}", cfg.SizeSmallBlocks));
                UpdateStatus(string.Format("File Blocks:         0x{0:X}", cfg.FSBlocks));
            }
            if (verboselevel >= 1)
            {
                UpdateStatus(string.Format(new NumberFormatInfo
                {
                    NumberGroupSeparator = " ",
                    NumberDecimalDigits = 0
                }, "Size Bytes:          {0:N} B", cfg.SizeBytes));
                UpdateStatus(string.Format(new NumberFormatInfo
                {
                    NumberGroupSeparator = " ",
                    NumberDecimalDigits = 0
                }, "Size Bytes (RAW):    {0:N} B", cfg.SizeRawBytes));
                UpdateStatus(string.Format("Size Readable:       {0}", GetSizeReadable(cfg.SizeBytes)));
                UpdateStatus(string.Format("Size Readable (RAW): {0}", GetSizeReadable(cfg.SizeBytes)));
            }
            if (verboselevel >= 3)
            {
                UpdateStatus(string.Format("Controller Type:     {0}", cfg.ControllerType));
                UpdateStatus(string.Format("Block Type:          {0}", cfg.BlockType));
            }
        }

    }
}