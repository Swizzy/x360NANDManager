namespace x360NANDManager {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Windows.Forms;
    using x360NANDManager.Properties;
    using x360NANDManager.SPI;

    public static class Main {
        private const string BaseName = "DLL v{0}.{1} (Build: {2}) {3}";
        private static readonly Version Ver = Assembly.GetExecutingAssembly().GetName().Version;
        private static NANDPro _arm;
        private static JRProgrammer _jrp;
        private static PICFlash _pic;
        private static MTX _mtx;
        private static bool _abort;

        static Main() {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        }

        public static string Version {
            get {
#if DEBUG
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, "DEBUG BUILD");
#elif ALPHA
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, Ver.Revision > 0 ? string.Format("ALPHA{0}", Ver.Revision) : "ALPHA");
#elif BETA
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, Ver.Revision > 0 ? string.Format("BETA{0}", Ver.Revision) : "BETA");
#else
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, "");
#endif
            }
        }

        public static void AbortOperation() {
            _abort = true;
        }

        public static event EventHandler<EventArg<string>> Status;
        public static event EventHandler<EventArg<string>> Debug;
        public static event EventHandler<EventArg<string>> Error;
        public static event EventHandler<EventArg<ProgressData>> Progress;

        private static void UpdateProgress(long current, long max) {
            if(Progress != null) {
                Progress(null, new EventArg<ProgressData>(new ProgressData {
                                                                           Current = current, Maximum = max, Percentage = Utils.GetPercentage(current + 1, max)
                                                                           }));
            }
        }

        internal static void UpdateStatus(string message) {
            if(Status != null && message != null)
                Status(null, new EventArg<string>(message));
        }

        internal static void SendError(string message) {
            if(Error != null && message != null)
                Error(null, new EventArg<string>(message));
            SendDebug(message);
        }

        [Conditional("DEBUG")] [Conditional("ALPHA")] [Conditional("BETA")] internal static void SendDebug(string message) {
            if(Debug != null && message != null)
                Debug(null, new EventArg<string>(message));
        }

        private static Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args) {
            if(string.IsNullOrEmpty(args.Name))
                throw new Exception("DLL Read Failure (Nothing to load!)");
            var name = string.Format("{0}.dll", args.Name.Split(',')[0]);
            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("{0}.{1}", typeof(Main).Namespace, name))) {
                if(stream != null) {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return Assembly.Load(data);
                }
                throw new Exception(string.Format("Can't find external nor internal {0}!", name));
            }
        }

        private static bool Init(OperationArgs args) {
            #region Class\Device Init

            switch(args.DeviceType) {
                case OperationArgs.DeviceModes.ARM:
                    _arm = new NANDPro();
                    if(!_arm.Initialized) {
                        SendError("Device Init Failed");
                        return false;
                    }
                    break;
                case OperationArgs.DeviceModes.JRProgrammer:
                    _jrp = new JRProgrammer();
                    if(!_jrp.Initialized) {
                        SendError("Device Init Failed");
                        return false;
                    }
                    break;
                case OperationArgs.DeviceModes.PICFlash:
                    _pic = new PICFlash();
                    if(!_pic.Initialized) {
                        SendError("Device Init Failed");
                        return false;
                    }
                    break;
                case OperationArgs.DeviceModes.MTX:
                    _mtx = new MTX();
                    if(!_mtx.Initialized) {
                        SendError("Device Init Failed");
                        return false;
                    }
                    break;
                case OperationArgs.DeviceModes.FTDI:
                    break;
            }

            #endregion Class\Device Init

            if(args.DeviceType != OperationArgs.DeviceModes.MMC) {
                uint config = 0;

                #region Get config

                switch(args.DeviceType) {
                    case OperationArgs.DeviceModes.ARM:
                        config = _arm.FlashInit();
                        break;
                    case OperationArgs.DeviceModes.JRProgrammer:
                        config = _jrp.FlashInit();
                        break;
                    case OperationArgs.DeviceModes.PICFlash:
                        config = _pic.FlashInit();
                        break;
                    case OperationArgs.DeviceModes.MTX:
                        config = _mtx.FlashInit();
                        break;
                    case OperationArgs.DeviceModes.FTDI:
                        break;
                }

                #endregion Get config

                args.Config = new XConfig(config);
                if(args.Config.Status != XConfig.ErrorLevel.Success) {
                    switch(args.Config.Status) {
                        case XConfig.ErrorLevel.BadConfig:
                            SendError("ERROR: Unable to find FlashConfig");
                            break;
                        case XConfig.ErrorLevel.UnkownControllerType:
                            SendError("ERROR: Unknown Flash Controller Type");
                            break;
                        case XConfig.ErrorLevel.UnkownBlockType:
                            SendError("ERROR: Unknown Block Type");
                            break;
                        case XConfig.ErrorLevel.UnsupportedBlockType:
                            SendError("ERROR: Unsupported Block Type");
                            break;
                        default:
                            SendError("Unkown error");
                            break;
                    }
                    return false;
                }
                switch(args.DeviceType) {
                    case OperationArgs.DeviceModes.ARM:
                        UpdateStatus(string.Format("Arm Version: {0}", _arm.ArmVersion));
                        break;
                    case OperationArgs.DeviceModes.JRProgrammer:
                        UpdateStatus(string.Format("Arm Version: {0}", _jrp.ArmVersion));
                        break;
                    case OperationArgs.DeviceModes.PICFlash:
                        UpdateStatus(string.Format("Arm Version: {0}", _pic.ArmVersion));
                        break;
                    case OperationArgs.DeviceModes.MTX:
                        UpdateStatus(string.Format("Arm Version: {0}", _mtx.ArmVersion));
                        break;
                }
                args.Config.PrintXConfig(args.VerbosityLevel);
                if(args.BlockCount <= 0)
                    args.BlockCount = (uint) (args.Config.SmallBlocksCount - args.StartBlock);
                if((args.Operation & OperationArgs.Operations.Write) == OperationArgs.Operations.Write) {
                    var fileBlocks = BlockUtils.GetFileBlockCount(args.File);
                    if(fileBlocks == 0)
                        args.BlockCount = (uint) (args.Config.SmallBlocksCount + 1);
                    else if(fileBlocks < args.BlockCount)
                        args.BlockCount = fileBlocks;
                }
                if(args.BlockCount - args.StartBlock > args.Config.SmallBlocksCount) {
                    switch(args.DeviceType) {
                        case OperationArgs.DeviceModes.ARM:
                            _arm.Release();
                            break;
                        case OperationArgs.DeviceModes.JRProgrammer:
                            _jrp.Release();
                            break;
                        case OperationArgs.DeviceModes.PICFlash:
                            _pic.Release();
                            break;
                        case OperationArgs.DeviceModes.MTX:
                            _mtx.Release();
                            break;
                    }
                    return false;
                }
            }
            return true;
        }

        public static bool ReadARM(string target, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, File = target, Operation = OperationArgs.Operations.Read, DumpCount = 1
                                                                        };
            return Init(args) && DoWork(args);
        }

        public static bool ReadARM(IEnumerable<string> files, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Read
                                                                        };
            args.Files.Clear();
            args.Files.AddRange(files);
            Utils.RemoveDuplicatesInList(ref args.Files);
            args.DumpCount = args.Files.Count;
            return Init(args) && DoWork(args);
        }

        public static bool ReadJRP(string target, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.JRProgrammer) {
                                                                                 StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, File = target, Operation = OperationArgs.Operations.Read, DumpCount = 1
                                                                                 };
            return Init(args) && DoWork(args);
        }

        public static bool ReadJRP(IEnumerable<string> files, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.JRProgrammer) {
                                                                                 StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Read
                                                                                 };
            args.Files.Clear();
            args.Files.AddRange(files);
            Utils.RemoveDuplicatesInList(ref args.Files);
            args.DumpCount = args.Files.Count;
            return Init(args) && DoWork(args);
        }

        public static bool ReadPIC(string target, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.PICFlash) {
                                                                             StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, File = target, Operation = OperationArgs.Operations.Read, DumpCount = 1
                                                                             };
            return Init(args) && DoWork(args);
        }

        public static bool ReadPIC(IEnumerable<string> files, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.PICFlash) {
                                                                             StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Read
                                                                             };
            args.Files.Clear();
            args.Files.AddRange(files);
            Utils.RemoveDuplicatesInList(ref args.Files);
            args.DumpCount = args.Files.Count;
            return Init(args) && DoWork(args);
        }

        public static bool ReadMTX(string target, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.MTX) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, File = target, Operation = OperationArgs.Operations.Read, DumpCount = 1
                                                                        };
            return Init(args) && DoWork(args);
        }

        public static bool ReadMTX(IEnumerable<string> files, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.MTX) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Read
                                                                        };
            args.Files.Clear();
            args.Files.AddRange(files);
            Utils.RemoveDuplicatesInList(ref args.Files);
            args.DumpCount = args.Files.Count;
            return Init(args) && DoWork(args);
        }

        public static bool WriteARM(string source, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0, bool addSpare = false, bool correctSpare = false, bool eraseWrite = false, bool verify = false) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Write, File = source
                                                                        };
            if(addSpare)
                args.Operation |= OperationArgs.Operations.AddSpare;
            else if(correctSpare)
                args.Operation |= OperationArgs.Operations.CorrectSpare;
            if(eraseWrite)
                args.Operation |= OperationArgs.Operations.Erase;
            if(verify)
                args.Operation |= OperationArgs.Operations.Verify;
            return Init(args) && DoWork(args);
        }

        public static bool WriteJRP(string source, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0, bool addSpare = false, bool correctSpare = false, bool eraseWrite = false, bool verify = false) {
            var args = new OperationArgs(OperationArgs.DeviceModes.JRProgrammer) {
                                                                                 StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Write, File = source
                                                                                 };
            if(addSpare)
                args.Operation |= OperationArgs.Operations.AddSpare;
            else if(correctSpare)
                args.Operation |= OperationArgs.Operations.CorrectSpare;
            if(eraseWrite)
                args.Operation |= OperationArgs.Operations.Erase;
            if(verify)
                args.Operation |= OperationArgs.Operations.Verify;
            return Init(args) && DoWork(args);
        }

        public static bool WritePIC(string source, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0, bool addSpare = false, bool correctSpare = false, bool eraseWrite = false, bool verify = false) {
            var args = new OperationArgs(OperationArgs.DeviceModes.PICFlash) {
                                                                             StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Write, File = source
                                                                             };
            if(addSpare)
                args.Operation |= OperationArgs.Operations.AddSpare;
            else if(correctSpare)
                args.Operation |= OperationArgs.Operations.CorrectSpare;
            if(eraseWrite)
                args.Operation |= OperationArgs.Operations.Erase;
            if(verify)
                args.Operation |= OperationArgs.Operations.Verify;
            return Init(args) && DoWork(args);
        }

        public static bool WriteMTX(string source, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0, bool addSpare = false, bool correctSpare = false, bool eraseWrite = false, bool verify = false) {
            var args = new OperationArgs(OperationArgs.DeviceModes.MTX) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Write, File = source
                                                                        };
            if(addSpare)
                args.Operation |= OperationArgs.Operations.AddSpare;
            else if(correctSpare)
                args.Operation |= OperationArgs.Operations.CorrectSpare;
            if(eraseWrite)
                args.Operation |= OperationArgs.Operations.Erase;
            if(verify)
                args.Operation |= OperationArgs.Operations.Verify;
            return Init(args) && DoWork(args);
        }

        public static bool EraseARM(uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Erase
                                                                        };
            return Init(args) && DoWork(args);
        }

        public static bool EraseJRP(uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.JRProgrammer) {
                                                                                 StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Erase
                                                                                 };
            return Init(args) && DoWork(args);
        }

        public static bool ErasePIC(uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.PICFlash) {
                                                                             StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Erase
                                                                             };
            return Init(args) && DoWork(args);
        }

        public static bool EraseMTX(uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.MTX) {
                                                                        StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Erase
                                                                        };
            return Init(args) && DoWork(args);
        }

        private static bool DoWork(OperationArgs args) {
            try {
                _abort = false;
                var ret = true;
                var maxBlocks = (args.StartBlock + args.BlockCount);
                switch(args.Operation) {
                        #region Read

                    case OperationArgs.Operations.Read:
                        maxBlocks = (uint) (maxBlocks * args.DumpCount);
                        for(var count = 0; count < args.DumpCount; count++) {
                            BinaryWriter bw;
                            if(args.DumpCount <= 1 && !string.IsNullOrEmpty(args.File)) {
                                bw = OpenWriter(args.File);
                                UpdateStatus(string.Format("Reading Blocks: 0x{0:X} to 0x{1:X}{2}Saving data to: {3}", args.StartBlock, args.StartBlock + args.BlockCount - 1, Environment.NewLine, args.File));
                            }
                            else {
                                bw = OpenWriter(args.Files[count]);
                                UpdateStatus(string.Format("Reading Blocks: 0x{0:X} to 0x{1:X}{2}Saving data to: {3}", args.StartBlock, args.StartBlock + args.BlockCount - 1, Environment.NewLine, args.Files[count]));
                            }
                            if(bw == null)
                                return false;
                            if(args.DeviceType != OperationArgs.DeviceModes.MMC) {
                                for(var block = args.StartBlock; block < args.StartBlock + args.BlockCount; block++) {
                                    if(_abort) {
                                        bw.Close();
                                        return false;
                                    }
                                    UpdateProgress(block + (count == 0 ? 0 : ((maxBlocks / args.DumpCount) * count)), maxBlocks);
                                    var data = new byte[0];
                                    switch(args.DeviceType) {
                                        case OperationArgs.DeviceModes.ARM:
                                            data = _arm.FlashRead(block);
                                            break;
                                        case OperationArgs.DeviceModes.JRProgrammer:
                                            data = _jrp.FlashRead(block);
                                            break;
                                        case OperationArgs.DeviceModes.PICFlash:
                                            data = _pic.FlashRead(block);
                                            break;
                                        case OperationArgs.DeviceModes.MTX:
                                            data = _mtx.FlashRead(block);
                                            break;
                                        case OperationArgs.DeviceModes.FTDI:
                                            data = FTDI.FlashRead(block);
                                            break;
                                    }
                                    if(data.Length > 0)
                                        bw.Write(data);
                                    else {
                                        ret = false;
                                        SendError(string.Format("Read Failed on block: 0x{0:X}", block));
                                    }
                                }
                            }
                            else {
                                //TODO: Add MMC Read
                            }
                            bw.Close();
                        }
                        break;

                        #endregion Read

                        #region Erase

                    case OperationArgs.Operations.Erase:
                        if(args.DeviceType != OperationArgs.DeviceModes.MMC) {
                            UpdateStatus(string.Format("Erasing Blocks: 0x{0:X} to 0x{1:X}", args.StartBlock, args.StartBlock + args.BlockCount - 1));
                            for(var block = args.StartBlock; block < args.StartBlock + args.BlockCount; block++) {
                                if(_abort)
                                    return false;
                                UpdateProgress(block, args.StartBlock + args.BlockCount);
                                switch(args.DeviceType) {
                                    case OperationArgs.DeviceModes.ARM:
                                        if(!_arm.FlashErase(block)) {
                                            ret = false;
                                            SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                        }
                                        break;
                                    case OperationArgs.DeviceModes.JRProgrammer:
                                        if(!_jrp.FlashErase(block)) {
                                            ret = false;
                                            SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                        }
                                        break;
                                    case OperationArgs.DeviceModes.PICFlash:
                                        if(!_pic.FlashErase(block)) {
                                            ret = false;
                                            SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                        }
                                        break;
                                    case OperationArgs.DeviceModes.MTX:
                                        if(!_mtx.FlashErase(block)) {
                                            ret = false;
                                            SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                        }
                                        break;
                                    case OperationArgs.DeviceModes.FTDI:
                                        if(!FTDI.FlashErase(block)) {
                                            ret = false;
                                            SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                        }
                                        break;
                                }
                            }
                        }
                        else {
                            //TODO: Add MMC Erase
                        }
                        break;

                        #endregion Erase

                        #region Write

                    default:

                        if((args.Operation & OperationArgs.Operations.Write) == OperationArgs.Operations.Write) {
                            UpdateStatus("Writing with the following parameters:");
                            var verify = (args.Operation & OperationArgs.Operations.Verify) == OperationArgs.Operations.Verify;
                            var dataWritten = new List<byte>();
                            var br = OpenReader(args.File);
                            if(args.DeviceType != OperationArgs.DeviceModes.MMC) {
                                var eraseFirst = ((args.Operation & OperationArgs.Operations.Erase) == OperationArgs.Operations.Erase);
                                var addSpare = (args.Operation & OperationArgs.Operations.AddSpare) == OperationArgs.Operations.AddSpare;
                                var correctSpare = (args.Operation & OperationArgs.Operations.CorrectSpare) == OperationArgs.Operations.CorrectSpare;
                                UpdateStatus(string.Format("Erase before write: {0}", eraseFirst ? "Enabled" : "Disabled"));
                                UpdateStatus(string.Format("Verify after write: {0}", verify ? "Enabled" : "Disabled"));
                                UpdateStatus(string.Format("Write Mode: {0}", addSpare ? "Add Spare" : correctSpare ? "Correct Spare" : "RAW"));
                                maxBlocks *= verify && eraseFirst ? 3 : (uint) (verify || eraseFirst ? 2 : 1);

                                #region Erase Before Write

                                if(eraseFirst) {
                                    UpdateStatus(string.Format("Erasing Blocks: 0x{0:X} to 0x{1:X}", args.StartBlock, args.StartBlock + args.BlockCount - 1));
                                    for(var block = args.StartBlock; block < args.StartBlock + args.BlockCount; block++) {
                                        if(_abort) {
                                            br.Close();
                                            return false;
                                        }
                                        UpdateProgress(block, maxBlocks);
                                        switch(args.DeviceType) {
                                            case OperationArgs.DeviceModes.ARM:
                                                if(!_arm.FlashErase(block)) {
                                                    ret = false;
                                                    SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                                }
                                                break;
                                            case OperationArgs.DeviceModes.JRProgrammer:
                                                if(!_jrp.FlashErase(block)) {
                                                    ret = false;
                                                    SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                                }
                                                break;
                                            case OperationArgs.DeviceModes.PICFlash:
                                                if(!_pic.FlashErase(block)) {
                                                    ret = false;
                                                    SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                                }
                                                break;
                                            case OperationArgs.DeviceModes.MTX:
                                                if(!_mtx.FlashErase(block)) {
                                                    ret = false;
                                                    SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                                }
                                                break;
                                            case OperationArgs.DeviceModes.FTDI:
                                                if(!FTDI.FlashErase(block)) {
                                                    ret = false;
                                                    SendError(string.Format("Erase Failed on block: 0x{0:X}", block));
                                                }
                                                break;
                                        }
                                    }
                                }

                                #endregion Erase Before Write

                                #region Write

                                if(eraseFirst) {
                                    UpdateStatus("Cycling device before write...");
                                    switch(args.DeviceType) {
                                        case OperationArgs.DeviceModes.ARM:
                                            if(_arm.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.JRProgrammer:
                                            if(_jrp.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.PICFlash:
                                            if(_pic.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.MTX:
                                            if(_mtx.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                    }
                                }

                                UpdateStatus(string.Format("Writing data from File: {2}{3}Blocks: 0x{0:X} to 0x{1:X}", args.StartBlock, args.StartBlock + args.BlockCount - 1, args.File, Environment.NewLine));
                                for(var block = args.StartBlock; block < args.StartBlock + args.BlockCount; block++) {
                                    if(_abort) {
                                        br.Close();
                                        return false;
                                    }
                                    UpdateProgress(block + (eraseFirst && verify ? maxBlocks / 3 : eraseFirst ? maxBlocks / 2 : 0), maxBlocks);
                                    var data = br.ReadBytes(!addSpare ? 0x4200 : 0x4000);
                                    if(addSpare)
                                        data = BlockUtils.AddSpareBlock(ref data, block, args.Config.MetaType);
                                    else if(correctSpare)
                                        BlockUtils.CorrectSpareBlock(ref data, block, args.Config.MetaType);
                                    switch(args.DeviceType) {
                                        case OperationArgs.DeviceModes.ARM:
                                            if(!_arm.FlashWrite(block, data)) {
                                                ret = false;
                                                SendError(string.Format("Write Failed on block: 0x{0:X}", block));
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.JRProgrammer:
                                            if(!_jrp.FlashWrite(block, data)) {
                                                ret = false;
                                                SendError(string.Format("Write Failed on block: 0x{0:X}", block));
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.PICFlash:
                                            if(!_pic.FlashWrite(block, data)) {
                                                ret = false;
                                                SendError(string.Format("Write Failed on block: 0x{0:X}", block));
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.MTX:
                                            if(!_mtx.FlashWrite(block, data)) {
                                                ret = false;
                                                SendError(string.Format("Write Failed on block: 0x{0:X}", block));
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.FTDI:
                                            if(!FTDI.FlashWrite(block, data)) {
                                                ret = false;
                                                SendError(string.Format("Write Failed on block: 0x{0:X}", block));
                                            }
                                            break;
                                    }
                                    if(verify)
                                        dataWritten.AddRange(data);
                                }

                                #endregion Write

                                #region Verify

                                if(verify) {
                                    UpdateStatus("Cycling device before verify...");
                                    switch(args.DeviceType) {
                                        case OperationArgs.DeviceModes.ARM:
                                            if(_arm.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.JRProgrammer:
                                            if(_jrp.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.PICFlash:
                                            if(_pic.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.MTX:
                                            if(_mtx.DeviceCycle() != args.Config.Config) {
                                                SendError("Config changed! Aborting...");
                                                return false;
                                            }
                                            break;
                                    }
                                    UpdateStatus(string.Format("Verifying Blocks: 0x{0:X} to 0x{1:X}", args.StartBlock, args.StartBlock + args.BlockCount - 1));
                                    var data = new byte[0];
                                    var offset = 0;
                                    var dataList = dataWritten.ToArray();
                                    dataWritten.Clear();
                                    for(var block = args.StartBlock; block < args.StartBlock + args.BlockCount; block++) {
                                        if(_abort) {
                                            br.Close();
                                            return false;
                                        }
                                        var doVerify = true;
                                        UpdateProgress(block + (eraseFirst ? ((maxBlocks / 3) * 2) : (maxBlocks / 2)), maxBlocks);
                                        switch(args.DeviceType) {
                                            case OperationArgs.DeviceModes.ARM:
                                                data = _arm.FlashRead(block);
                                                doVerify = _arm.Status == 0x200;
                                                break;
                                            case OperationArgs.DeviceModes.JRProgrammer:
                                                data = _jrp.FlashRead(block);
                                                doVerify = _jrp.Status == 0x200;
                                                break;
                                            case OperationArgs.DeviceModes.PICFlash:
                                                data = _pic.FlashRead(block);
                                                doVerify = _pic.Status == 0x200;
                                                break;
                                            case OperationArgs.DeviceModes.MTX:
                                                data = _mtx.FlashRead(block);
                                                doVerify = _mtx.Status == 0x200;
                                                break;
                                            case OperationArgs.DeviceModes.FTDI:
                                                data = FTDI.FlashRead(block);
                                                doVerify = FTDI.Status == 0x200;
                                                break;
                                        }
                                        if(data.Length <= 0) {
                                            SendError(string.Format("Read Failed on block: 0x{0:X}", block));
                                            return false;
                                        }
                                        if(doVerify && !Utils.CompareByteArrays(data, dataList, offset)) {
                                            SendError(string.Format("Verify of block 0x{0:X} Failed!", block));
                                            ret = false;
                                        }
                                        offset += data.Length;
                                    }
                                }

                                #endregion Verify
                            }
                            else {
                                //TODO: Add MMC Write
                            }
                            br.Close();
                        }
                        else {
                            SendError("Invalid Operation Detected");
                            return false;
                        }
                        break;

                        #endregion
                }
                return ret;
            }
            catch(Exception ex) {
                SendError(ex.Message);
                return false;
            }
            finally {
                switch(args.DeviceType) {
                    case OperationArgs.DeviceModes.ARM:
                        _arm.FlashDeInit();
                        _arm.Release();
                        break;
                    case OperationArgs.DeviceModes.JRProgrammer:
                        _jrp.FlashDeInit();
                        _jrp.Release();
                        break;
                    case OperationArgs.DeviceModes.PICFlash:
                        _pic.FlashDeInit();
                        _pic.Release();
                        break;
                    case OperationArgs.DeviceModes.MTX:
                        _mtx.FlashDeInit();
                        _mtx.Release();
                        break;
                    case OperationArgs.DeviceModes.FTDI:
                        break;
                }
            }
        }

        private static BinaryReader OpenReader(string file) {
            try {
                return new BinaryReader(File.Open(file, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read));
            }
            catch(Exception) {
                if(MessageBox.Show(string.Format(Resources.OpenReadFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error) == DialogResult.Yes)
                    return OpenReader(file);
            }
            return null;
        }

        private static BinaryWriter OpenWriter(string file) {
            try {
                return new BinaryWriter(File.Open(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
            }
            catch(Exception) {
                if(MessageBox.Show(string.Format(Resources.OpenWriteFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error) == DialogResult.Yes)
                    return OpenWriter(file);
            }
            return null;
        }

        #region Nested type: OperationArgs

        internal sealed class OperationArgs {
            #region DeviceModes enum

            public enum DeviceModes {
                FTDI,
                ARM,
                JRProgrammer,
                PICFlash,
                MMC,
                MTX
            }

            #endregion

            #region Operations enum

            [Flags] public enum Operations {
                None = 0x0,
                Read = 0x1,
                Erase = 0x2,
                Write = 0x4,
                Verify = 0x8,
                XSVF = 0x10,
                AddSpare = 0x20,
                CorrectSpare = 0x40,
                AutoRemap = 0x80
            }

            #endregion

            public readonly DeviceModes DeviceType;
            public uint BlockCount;
            public XConfig Config;
            public int DumpCount = 1;
            public string File;
            public List<string> Files = new List<string>();
            public long MMCOffset;
            public long MMCSize;
            public Operations Operation;
            public uint StartBlock;
            public int VerbosityLevel;

            public OperationArgs(DeviceModes deviceType) {
                DeviceType = deviceType;
            }
        }

        #endregion
    }
}