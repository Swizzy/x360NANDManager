namespace x360NANDManager {
    using System;
    using System.Collections.Generic;
    using System.Reflection;

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
        public static event EventHandler<EventArg<double>> Progress;

        internal static void UpdateProgress(double progress) {
            if(Progress != null)
                Progress(null, new EventArg<double>(progress));
        }

        internal static void UpdateStatus(string message) {
            if(Status != null && message != null)
                Status(null, new EventArg<string>(message));
        }

        internal static void SendError(string message) {
            if(Error != null && message != null)
                Error(null, new EventArg<string>(message));
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
                var cfg = new XConfig(config);
                if(cfg.Status != XConfig.ErrorLevel.Success) {
                    switch(cfg.Status) {
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
                cfg.PrintXConfig(args.VerbosityLevel);
                if(args.BlockCount <= 0)
                    args.BlockCount = (uint) (cfg.SizeBlocks - args.StartBlock);
                else if(args.BlockCount - args.StartBlock > cfg.SizeBlocks) {
                    if(args.DeviceType == OperationArgs.DeviceModes.ARM)
                        NANDPro.Release();
                    return false;
                }
            }
            return true;
        }

        public static bool ReadARM(string target, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) { StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, File = target, Operation = OperationArgs.Operations.Read };
            if(!Init(args))
                return false;
            //TODO: Add Single Read
            return true;
        }

        public static bool ReadARM(IEnumerable<string> files, uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0) {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) { StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Read };
            args.Files.Clear();
            args.Files.AddRange(files);
            if (!Init(args))
                return false;
            //TODO: Add Multi Read
            return true;
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
            if(!Init(args))
                return false;
            //TODO: Add Write
            return true;
        }

        public static bool EraseARM(uint startBlock = 0, uint blockCount = 0, int verbosityLevel = 0)
        {
            var args = new OperationArgs(OperationArgs.DeviceModes.ARM) { StartBlock = startBlock, BlockCount = blockCount, VerbosityLevel = verbosityLevel, Operation = OperationArgs.Operations.Erase };
            if (!Init(args))
                return false;
            //TODO: Add Erase
            return true;
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
            public readonly List<string> Files = new List<string>();
            public uint BlockCount;
            public int DumpCount = 1;
            public string File;
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