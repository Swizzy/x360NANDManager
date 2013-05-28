namespace x360NANDManager {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Windows.Forms;
    using x360NANDManager.Properties;

    public static class Main {
        private const string BaseName = "x360NANDManager v{0}.{1} (Build: {2}) {3}";
        private static readonly Version Ver = Assembly.GetExecutingAssembly().GetName().Version;

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

        public static event EventHandler<EventArg<string>> Status;
        public static event EventHandler<EventArg<string>> Error;
        public static event EventHandler<EventArg<ProgressData>> Progress;

        internal static void UpdateProgress(long current, long max) {
            if(Progress != null)
                Progress(null, new EventArg<ProgressData>(new ProgressData { Current = current, Maximum = max, Percentage = Utils.GetPercentage(current + 1, max)}));
        }

        internal static void UpdateStatus(string message) {
            if(Status != null && message != null)
                Status(null, new EventArg<string>(message));
        }

        internal static void SendError(string message) {
            if(Error != null && message != null)
                Error(null, new EventArg<string>(message));
        }

        private static void SendBlockError(string operation, uint block, uint error) {
            SendError(string.Format("ERROR: 0x{0:X} {1} block 0x{2:X}", error, operation, block));
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
            if(args.DeviceType == OperationArgs.DeviceModes.ARM) {
                if(!NANDPro.Initialized && !NANDPro.Init()) {
                    SendError("Device Init Failed");
                    return false;
                }
            }
            else if(args.DeviceType == OperationArgs.DeviceModes.FTDI) {
                //TODO: add FTDI init
            }
            if(args.DeviceType == OperationArgs.DeviceModes.ARM || args.DeviceType == OperationArgs.DeviceModes.FTDI) {
                uint config = 0;
                if(args.DeviceType == OperationArgs.DeviceModes.ARM)
                    config = NANDPro.FlashInit();
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
                if (args.DeviceType == OperationArgs.DeviceModes.ARM)
                    UpdateStatus(string.Format("Arm Version: {0}", NANDPro.ArmVersion));
                args.Config.PrintXConfig(args.VerbosityLevel);
                if(args.BlockCount <= 0)
                    args.BlockCount = (uint) (args.Config.SizeBlocks - args.StartBlock);
                else if(args.BlockCount - args.StartBlock > args.Config.SizeBlocks) {
                    if(args.DeviceType == OperationArgs.DeviceModes.ARM)
                        NANDPro.Release();
                    return false;
                }
            }
            return true;
        }

        public static bool ReadARM(string target, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) {
                                                                            StartBlock = startBlock,
                                                                            BlockCount = blockCount,
                                                                            VerbosityLevel =
                                                                                verbosityLevel,
                                                                            File = target,
                                                                            Operation =
                                                                                OperationArgs.
                                                                                Operations.Read,
                                                                            DumpCount = 1
                                                                        };
            return Init(args) && DoWork(args);
        }

        public static bool ReadARM(IEnumerable<string> files, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) { StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Read };
            args.Files.Clear();
            args.Files.AddRange(files);
            Utils.RemoveDuplicatesInList(ref args.Files);
            args.DumpCount = args.Files.Count;
            return Init(args) && DoWork(args);
        }

        public static bool WriteARM(string source, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0, bool addSpare = false, bool correctSpare = false, bool eraseWrite = true, bool verify = false) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) { StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Write, File = source };
            if (addSpare)
                args.Operation |= OperationArgs.Operations.AddSpare;
            else if (correctSpare)
                args.Operation |= OperationArgs.Operations.CorrectSpare;
            if (eraseWrite)
                args.Operation |= OperationArgs.Operations.Erase;
            if (verify)
                args.Operation |= OperationArgs.Operations.Verify;
            return Init(args) && DoWork(args);
        }

        public static bool EraseARM(uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0)
        {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) { StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Erase };
            if (!Init(args))
                return false;
            //TODO: Add Erase
            return true;
        }

        private static bool DoWork(OperationArgs args) {
            try {
                switch (args.Operation) {
                    #region Read

                    case OperationArgs.Operations.Read:
                        for (var count = 0; count < args.DumpCount; count++) {
                            BinaryWriter bw;
                            if (args.DumpCount <= 1 && !string.IsNullOrEmpty(args.File))
                                bw = OpenWriter(args.File);
                            else
                                bw = OpenWriter(args.Files[count]);
                            if (bw == null)
                                return false;
                            if (args.DeviceType != OperationArgs.DeviceModes.MMC) {
                                for (var block = args.StartBlock;
                                     block < args.StartBlock + args.BlockCount;
                                     block++) {
                                    UpdateProgress(block, args.StartBlock + args.BlockCount);
                                    byte[] data;
                                    switch (args.DeviceType) {
                                        case OperationArgs.DeviceModes.ARM:
                                            data = NANDPro.FlashRead(block);
                                            if (NANDPro.Status != 0x200)
                                                SendBlockError("Reading", block, NANDPro.Status);
                                            bw.Write(data);
                                            break;
                                        case OperationArgs.DeviceModes.FTDI:
                                            data = FTDI.FlashRead(block);
                                            if (FTDI.Status != 0x200)
                                                SendBlockError("Reading", block, FTDI.Status);
                                            bw.Write(data);
                                            break;
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
                        if (args.DeviceType != OperationArgs.DeviceModes.MMC) {
                            for (var block = args.StartBlock;
                                 block < args.StartBlock + args.BlockCount;
                                 block++) {
                                UpdateProgress(block, args.StartBlock + args.BlockCount);
                                switch (args.DeviceType) {
                                    case OperationArgs.DeviceModes.ARM:
                                        NANDPro.FlashErase(block);
                                        if (NANDPro.Status != 0x200)
                                            SendBlockError("Erasing", block, NANDPro.Status);
                                        break;
                                    case OperationArgs.DeviceModes.FTDI:
                                        FTDI.FlashErase(block);
                                        if (FTDI.Status != 0x200)
                                            SendBlockError("Erasing", block, FTDI.Status);
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
                        if ((args.Operation & OperationArgs.Operations.Write) == OperationArgs.Operations.Write) {
                            var verify = (args.Operation & OperationArgs.Operations.Verify) == OperationArgs.Operations.Verify;
                            var br = OpenReader(args.File);
                            if (args.DeviceType != OperationArgs.DeviceModes.MMC)
                            {
                                var eraseFirst = (args.Operation & OperationArgs.Operations.Erase) == OperationArgs.Operations.Erase;
                                var addSpare = (args.Operation & OperationArgs.Operations.AddSpare) == OperationArgs.Operations.AddSpare;
                                var correctSpare = (args.Operation & OperationArgs.Operations.CorrectSpare) == OperationArgs.Operations.CorrectSpare;
                                for (var block = args.StartBlock; block < args.StartBlock + args.BlockCount; block++) {
                                    var doVerify = true;
                                    UpdateProgress(block, args.StartBlock + args.BlockCount);
                                    var data = br.ReadBytes(!addSpare ? 0x4200 : 0x4000);
                                    var tmp = new byte[0];
                                    if (addSpare)
                                        data = BlockUtils.AddSpareBlock(ref data, block, args.Config.MetaType);
                                    else if (correctSpare)
                                        BlockUtils.CorrectSpareBlock(ref data, block, args.Config.MetaType);
                                    switch (args.DeviceType)
                                    {
                                        case OperationArgs.DeviceModes.ARM:
                                            if (eraseFirst) {
                                                NANDPro.FlashErase(block);
                                                if (NANDPro.Status != 0x200)
                                                    SendBlockError("Erasing", block, NANDPro.Status);
                                            }
                                            if (!NANDPro.FlashWrite(block, data))
                                                SendError("Code Error...");
                                            if (NANDPro.Status != 0x200) {
                                                SendBlockError("Writing", block, NANDPro.Status);
                                                doVerify = false;
                                            }
                                            if (verify && doVerify) {
                                                tmp = NANDPro.FlashRead(block);
                                                if (NANDPro.Status != 0x200) {
                                                    SendBlockError("Verifying", block, NANDPro.Status);
                                                    doVerify = false;
                                                }
                                            }
                                            break;
                                        case OperationArgs.DeviceModes.FTDI:
                                            if (eraseFirst) {
                                                FTDI.FlashErase(block);
                                                if (FTDI.Status != 0x200)
                                                    SendBlockError("Erasing", block, FTDI.Status);
                                            }
                                            FTDI.FlashWrite(block, data);
                                            if (FTDI.Status != 0x200) {
                                                SendBlockError("Writing", block, FTDI.Status);
                                                doVerify = false;
                                            }
                                            if (verify && doVerify) {
                                                tmp = FTDI.FlashRead(block);
                                                if (FTDI.Status != 0x200) {
                                                    SendBlockError("Verifying", block, FTDI.Status);
                                                    doVerify = false;
                                                }
                                            }
                                            break;
                                    }
                                    if (verify && doVerify && !Utils.CompareByteArrays(tmp, data))
                                        SendError(string.Format("Verify of block 0x{0:X} Failed!", block));
                                }
                            }
                            else
                            {
                                //TODO: Add MMC Write
                            }
                            br.Close();
                        }
                        else {
                            SendError("Invalid Operation Detected");
                            return false;
                        }
                        break;

                    #endregion Write
                }
                return true;
            }
            catch (Exception ex) {
                SendError(ex.Message);
                return false;
            }
            finally {
                if(args.DeviceType == OperationArgs.DeviceModes.ARM)
                    NANDPro.FlashDeInit();
                    NANDPro.Release();
            }
        }

        private static BinaryReader OpenReader(string file) {
            try { return new BinaryReader(File.Open(file, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read)); }
            catch (Exception)
            {
                if (MessageBox.Show(string.Format(Resources.OpenReadFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error) == DialogResult.Yes)
                    return OpenReader(file);
            }
            return null;
        }

        private static BinaryWriter OpenWriter(string file) {
            try {
                return new BinaryWriter(File.Open(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
            }
            catch (Exception) {
                if (MessageBox.Show(string.Format(Resources.OpenWriteFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error) == DialogResult.Yes)
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
                MMC
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
            public List<string> Files = new List<string>();
            public uint StartBlock;
            public uint BlockCount;
            public long MMCOffset;
            public long MMCSize;
            public int DumpCount = 1;
            public string File;
            public Operations Operation;
            public int VerbosityLevel;
            public XConfig Config;

            public OperationArgs(DeviceModes deviceType) {
                DeviceType = deviceType;
            }
        }

        #endregion
    }
}