namespace x360NANDManager {
    using System;
    using System.Globalization;

    internal abstract class FlasherOutput : BlockUtils, IFlasherOutput {
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
        ///   Fires the <seealso cref="Progress" /> event with specified information
        /// </summary>
        /// <param name="current"> Current block </param>
        /// <param name="max"> Last Block </param>
        /// <param name="totalCurrent"> Current block (total blocks) </param>
        /// <param name="total"> Total Block (Used for calculating progress with multipile parts) </param>
        public void UpdateProgress(uint current, uint max, uint totalCurrent = 0, uint total = 0) {
            var prg = Progress;
            if(prg == null)
                return;
            if(total == 0)
                total = max;
            if(totalCurrent == 0)
                totalCurrent = current;
            prg(null, new EventArg<ProgressData>(new ProgressData {
                                                                  Current = current, Maximum = max, Percentage = ((double) (totalCurrent + 1) / total) * 100
                                                                  }));
        }


        /// <summary>
        ///   Fires the <seealso cref="Progress" /> event with specified information
        /// </summary>
        /// <param name="currentSector"> Current sector </param>
        /// <param name="lastSector"> Last sector </param>
        /// <param name="sectorSize"> Size in bytes of each sector (used to convert sectors to offset) </param>
        /// <param name="bufsize"> Size of buffer (used to correct curent sector information) </param>
        public void UpdateMMCProgress(long currentSector, long lastSector, int sectorSize, long bufsize) {
            var prg = Progress;
            if(prg == null)
                return;
            prg(null, new EventArg<ProgressData>(new ProgressData {
                Current = (currentSector * sectorSize) + bufsize,
                Maximum = (lastSector * sectorSize),
                Percentage = (((double)((currentSector * sectorSize) + bufsize) / (lastSector * sectorSize)) * 100)
            }));
        }

        /// <summary>
        ///   Fires the <seealso cref="Progress" /> event with specified information
        /// </summary>
        /// <param name="offset"> Current Offset </param>
        /// <param name="maximum"> Last Offset </param>
        /// <param name="bufsize"> Size of buffer (used to correct curent sector information) </param>
        public void UpdateMMCProgressEX(long offset, long maximum, long bufsize) {
            var prg = Progress;
            if(prg == null)
                return;
            prg(null, new EventArg<ProgressData>(new ProgressData {
                                                                  Current = offset + bufsize, Maximum = maximum, Percentage = ((double) (offset + bufsize) / maximum) * 100
                                                                  }));
        }

        /// <summary>
        ///   Fires the <seealso cref="Status" /> event with specified message
        /// </summary>
        /// <param name="message"> Message to send </param>
        /// <param name="args"> Optional parameters if you want to use it like printf or string.format </param>
        public void UpdateStatus(string message, params object[] args)
        {
            message = args.Length == 0 ? message : string.Format(message, args);
            var stat = Status;
            if(stat != null && !string.IsNullOrEmpty(message))
                stat(null, new EventArg<string>(message));
        }

        /// <summary>
        ///   Fires the <seealso cref="Error" /> event with specified message, it also forwards the message to the Debug event
        /// </summary>
        /// <param name="message"> Message to send </param>
        /// <param name="args"> Optional parameters if you want to use it like printf or string.format </param>
        public void SendError(string message, params object[] args)
        {
            message = args.Length == 0 ? message : string.Format(message, args);
            var err = Error;
            if(err == null || string.IsNullOrEmpty(message))
                return;
            err(null, new EventArg<string>(message));
            Main.SendDebug(message);
        }

        /// <summary>
        ///   Checks if the Flash Staus states that we've got a badblock, if <paramref name="verbose" /> is set to true the meaning of the status will be printed aswell
        /// </summary>
        /// <param name="status"> Flash Status to check </param>
        /// <param name="block"> BlockID for current block </param>
        /// <param name="operation"> Operation message (what are we doing?) </param>
        /// <param name="verbose"> Set to true for additional information about the status </param>
        /// <returns> True if the status says the block is bad </returns>
        public bool IsBadBlock(uint status, uint block, string operation, bool verbose = false) {
            if(status != 0x200 && status > 0) {
                SendError("ERROR: 0x{0:X} {1} block 0x{2:X}", status, operation, block);
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
            if(status == 0x200)
                return false; // This block is OK
            SendError("Unkown Status code (0) Encounterd! while {0} block 0x{1:X}", operation, block);
            return true;
        }

        #endregion IFlasherOutput Implementation

        /// <summary>
        ///   Prints the <seealso cref="XConfig" /> information
        /// </summary>
        /// <param name="cfg"> <seealso cref="XConfig" /> Containing information to be printed </param>
        /// <param name="verboselevel"> Verbosity level of the output </param>
        protected void PrintXConfig(XConfig cfg, int verboselevel = 0) {
            if(verboselevel >= 0)
                UpdateStatus("FlashConfig:         0x{0:X08}", cfg.Config);
            if(verboselevel >= 1) {
                UpdateStatus("Page Size:           0x{0:X}", cfg.PageSize);
                UpdateStatus("Meta Size:           0x{0:X}", cfg.MetaSize);
                UpdateStatus("Meta Type:           0x{0:X}", cfg.MetaType);
                UpdateStatus("Block Size (RAW):    0x{0:X}", cfg.BlockRawSize);
                UpdateStatus("Block Size:          0x{0:X}", cfg.BlockSize);
            }
            if(verboselevel >= 2)
                UpdateStatus("Pages Per Block:     {0}", cfg.PagesPerBlock);
            if(verboselevel >= 0)
                UpdateStatus("Size Blocks:         0x{0:X}", cfg.SizeBlocks);
            if(verboselevel >= 2) {
                UpdateStatus("Small BlocksCount:   0x{0:X}", cfg.SizeSmallBlocks);
                UpdateStatus("File Blocks:         0x{0:X}", cfg.FSBlocks);
            }
            if(verboselevel >= 1) {
                UpdateStatus(string.Format(new NumberFormatInfo {
                                                                NumberGroupSeparator = " ", NumberDecimalDigits = 0
                                                                }, "Size Bytes:          {0:N} B", cfg.SizeBytes));
                UpdateStatus(string.Format(new NumberFormatInfo {
                                                                NumberGroupSeparator = " ", NumberDecimalDigits = 0
                                                                }, "Size Bytes (RAW):    {0:N} B", cfg.SizeRawBytes));
                UpdateStatus("Size Readable:       {0}", GetSizeReadable(cfg.SizeBytes));
                UpdateStatus("Size Readable (RAW): {0}", GetSizeReadable(cfg.SizeRawBytes));
            }
            if(verboselevel >= 3) {
                UpdateStatus("Controller Type:     {0}", cfg.ControllerType);
                UpdateStatus("Block Type:          {0}", cfg.BlockType);
            }
        }
    }
}