namespace x360NANDManager {
    using System.Globalization;

    internal sealed class XConfig {
        #region ErrorLevel enum

        public enum ErrorLevel {
            Success,
            UnkownControllerType,
            UnkownBlockType,
            UnsupportedBlockType,
            BadConfig
        }

        #endregion

        public readonly int BlockSize;
        public readonly int BlockType;
        public readonly uint Config;
        public readonly int ControllerType;
        public readonly int FSBlocks;
        public readonly int MetaSize;
        public readonly int MetaType;
        public readonly int PageSize;
        public readonly int SizeBlocks;
        public readonly int SizeBytes;
        public readonly int SmallBlocksCount;
        public readonly ErrorLevel Status;

        public XConfig(uint config) {
            if (config == 0) {
                Status = ErrorLevel.BadConfig;
                return;
            }
            Config = config;
            PageSize = 0x200;
            MetaSize = 0x10;
            ControllerType = (int) ((config >> 17) & 3);
            BlockType = (int) ((config >> 4) & 3);
            switch (ControllerType) {
                case 0: // Small block original SFC (pre jasper)
                    switch (BlockType) {
                        case 0: // Unsupported 8MB?
                            Status = ErrorLevel.UnsupportedBlockType;
                            return;
                        case 1: // 16MB
                            SizeBlocks = 0x400;
                            FSBlocks = 0x3E0;
                            break;
                        case 2: // 32MB
                            SizeBlocks = 0x800;
                            FSBlocks = 0x7C0;
                            break;
                        case 3: // 64MB
                            SizeBlocks = 0x1000;
                            FSBlocks = 0xF80;
                            break;
                        default:
                            Status = ErrorLevel.UnsupportedBlockType;
                            return;
                    }
                    MetaType = 0;
                    BlockSize = 0x4000; // 16KB
                    SizeBytes = SizeBlocks << 0xE;
                    break;
                case 1: // New SFC/Southbridge: Codename "Panda"?
                case 2: // New SFC/Southbridge: Codename "Panda" v2?
                    switch (BlockType) {
                        case 0:
                        case 1:
                            if (ControllerType == 1 && BlockType == 0) {
                                Status = ErrorLevel.UnsupportedBlockType;
                                return;
                            }
                            MetaType = 1;
                            BlockSize = 0x4000; //16KB
                            if (ControllerType != 1 && BlockType == 1) {
                                // Small block 64MB setup
                                SizeBlocks = 0x1000;
                                FSBlocks = 0xF80;
                            }
                            else {
                                // Small block 16MB setup
                                SizeBlocks = 0x400;
                                FSBlocks = 0x3E0;
                            }
                            SizeBytes = SizeBlocks << 0xE;
                            break;
                        case 2:
                        case 3:
                            MetaType = 2;
                            SizeBytes = 0x1 <<
                                        (int)
                                        (((config >> 19) & 0x3) + ((config >> 21) & 0xF) + 0x17);
                            if (BlockType == 2) {
                                // Large Block: Current Jasper 256MB and 512MB
                                BlockSize = 0x20000; //128KB
                                FSBlocks = 0x1E0;
                                SizeBlocks = SizeBytes >> 0x11;
                            }
                            else {
                                // Large Block: Future or unknown hardware
                                BlockSize = 0x40000; //256KB
                                FSBlocks = 0xF0;
                                SizeBlocks = SizeBytes >> 0x12;
                            }
                            break;
                        default:
                            Status = ErrorLevel.UnkownBlockType;
                            return;
                    }
                    break;
                default:
                    Status = ErrorLevel.UnkownControllerType;
                    return;
            }
            SmallBlocksCount = (BlockSize*SizeBlocks)/0x4000;
            Status = ErrorLevel.Success;
        }

        public void PrintXConfig(int verboselevel = 0) {
            if (Status != ErrorLevel.Success)
                return;
            if (verboselevel >= 0)
                Main.UpdateStatus(string.Format("FlashConfig:       0x{0:X08}", Config));
            if (verboselevel >= 1) {
                Main.UpdateStatus(string.Format("PageSize:          0x{0:X}", PageSize));
                Main.UpdateStatus(string.Format("MetaSize:          0x{0:X}", MetaSize));
                Main.UpdateStatus(string.Format("MetaType:          0x{0:X}", MetaType));
                Main.UpdateStatus(string.Format("BlockSize:         0x{0:X}", BlockSize));
            }
            if (verboselevel >= 0)
                Main.UpdateStatus(string.Format("SizeInBlocks:      0x{0:X}", SizeBlocks));
            if (verboselevel >= 2) {
                Main.UpdateStatus(string.Format("SmallBlocksCount:  0x{0:X}", SmallBlocksCount));
                Main.UpdateStatus(string.Format("FileBlocks:        0x{0:X}", FSBlocks));
            }
            if (verboselevel >= 1)
                Main.UpdateStatus(string.Format(new NumberFormatInfo {
                                                                         NumberGroupSeparator = " ",
                                                                         NumberDecimalDigits = 0
                                                                     },
                                                "SizeInBytes:       {0:N} B",
                                                SizeBytes));
            if (verboselevel >= 0)
                Main.UpdateStatus(string.Format("SizeInReadable:    {0}", Utils.GetSizeReadable(SizeBytes)));
            if (verboselevel >= 3) {
                Main.UpdateStatus(string.Format("ControllerType:    {0}", ControllerType));
                Main.UpdateStatus(string.Format("BlockType:         {0}", BlockType));
            }
        }
    }
}