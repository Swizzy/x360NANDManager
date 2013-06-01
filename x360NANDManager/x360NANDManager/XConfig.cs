namespace x360NANDManager {
    using System;
    using System.Globalization;
    using System.IO;

    public sealed class XConfig : FlasherOutput {
        internal XConfig(uint config) {
            if(config == 0)
                throw new ArgumentException("Bad config value! (0)");
            Config = config;
            PageSize = 0x200;
            MetaSize = 0x10;
            ControllerType = ((config >> 17) & 3);
            BlockType = ((config >> 4) & 3);
            switch(ControllerType) {
                case 0: // Small block original SFC (pre jasper)
                    switch(BlockType) {
                        case 0: // Unsupported 8MB?
                            throw new Exception("Unsupported Controller Type & Block Type Combination");
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
                            throw new Exception("Unsupported Controller Type & Block Type Combination");
                    }
                    MetaType = 0;
                    BlockSize = 0x4000; // 16KB
                    SizeBytes = SizeBlocks << 0xE;
                    break;
                case 1: // New SFC/Southbridge: Codename "Panda"?
                case 2: // New SFC/Southbridge: Codename "Panda" v2?
                    switch(BlockType) {
                        case 0:
                        case 1:
                            if(ControllerType == 1 && BlockType == 0)
                                throw new Exception("Unsupported Controller Type & Block Type Combination");
                            MetaType = 1;
                            BlockSize = 0x4000; //16KB
                            if(ControllerType != 1 && BlockType == 1) {
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
                            SizeBytes = (uint) (0x1 << (int) (((config >> 19) & 0x3) + ((config >> 21) & 0xF) + 0x17));
                            if(BlockType == 2) {
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
                            throw new Exception("Unsupported Controller Type & Block Type Combination");
                    }
                    break;
                default:
                    throw new Exception("Unsupported Controller Type & Block Type Combination");
            }
            SizeSmallBlocks = (BlockSize * SizeBlocks) / 0x4000;
            PagesPerBlock = (BlockSize / PageSize);
            BlockRawSize = (BlockSize / PageSize) * (PageSize + MetaSize);
            SizeRawBytes = (SizeBytes / BlockSize) * BlockRawSize;
        }

        public uint BlockSize { get; private set; }
        public uint BlockRawSize { get; private set; }
        public uint BlockType { get; private set; }
        public uint Config { get; private set; }
        public uint ControllerType { get; private set; }
        public uint FSBlocks { get; private set; }
        public uint MetaSize { get; private set; }
        public uint MetaType { get; private set; }
        public uint PageSize { get; private set; }
        public uint PagesPerBlock { get; private set; }
        public uint SizeBlocks { get; private set; }
        public uint SizeSmallBlocks { get; private set; }
        public uint SizeBytes { get; private set; }
        public uint SizeRawBytes { get; private set; }

        public void PrintXConfig(int verboselevel = 0) {
            if(verboselevel >= 0)
                UpdateStatus(string.Format("FlashConfig:         0x{0:X08}", Config));
            if(verboselevel >= 1) {
                UpdateStatus(string.Format("Page Size:           0x{0:X}", PageSize));
                UpdateStatus(string.Format("Meta Size:           0x{0:X}", MetaSize));
                UpdateStatus(string.Format("Meta Type:           0x{0:X}", MetaType));
                UpdateStatus(string.Format("Block Size (RAW):    0x{0:X}", BlockRawSize));
                UpdateStatus(string.Format("Block Size:          0x{0:X}", BlockSize));
            }
            if(verboselevel >= 2)
                UpdateStatus(string.Format("Pages Per Block:     {0}", PagesPerBlock));
            if(verboselevel >= 0)
                UpdateStatus(string.Format("Size Blocks:         0x{0:X}", SizeBlocks));
            if(verboselevel >= 2) {
                UpdateStatus(string.Format("Small BlocksCount:   0x{0:X}", SizeSmallBlocks));
                UpdateStatus(string.Format("File Blocks:         0x{0:X}", FSBlocks));
            }
            if(verboselevel >= 1) {
                UpdateStatus(string.Format(new NumberFormatInfo {
                                                                NumberGroupSeparator = " ", NumberDecimalDigits = 0
                                                                }, "Size Bytes:          {0:N} B", SizeBytes));
                UpdateStatus(string.Format(new NumberFormatInfo {
                                                                NumberGroupSeparator = " ", NumberDecimalDigits = 0
                                                                }, "Size Bytes (RAW):    {0:N} B", SizeRawBytes));
                UpdateStatus(string.Format("Size Readable:       {0}", GetSizeReadable(SizeBytes)));
                UpdateStatus(string.Format("Size Readable (RAW): {0}", GetSizeReadable(SizeBytes)));
            }
            if(verboselevel >= 3) {
                UpdateStatus(string.Format("Controller Type:     {0}", ControllerType));
                UpdateStatus(string.Format("Block Type:          {0}", BlockType));
            }
        }

        internal uint BlocksToDataSize(uint blocks) {
            return (BlockRawSize * blocks);
        }

        internal uint FixBlockCount(uint startblock, uint blocks) {
            if(blocks == 0)
                return SizeSmallBlocks - startblock;
            if(blocks > SizeSmallBlocks - startblock)
                throw new ArgumentException("You cannot erase more blocks then the device have!");
            return blocks;
        }

        internal uint GetFileBlockCount(string file, uint blocks = 0) {
            var fi = new FileInfo(file);
            var ret = (PageSize + MetaSize) * 0x20;
            if(fi.Length % ret == 0) {
                ret = (uint) (fi.Length / ret);
                if(ret > SizeSmallBlocks)
                    throw new ArgumentException("File is to big!");
                if(blocks == 0 || blocks > ret)
                    return ret;
                return blocks;
            }
            ret = (PageSize * 0x20);
            if(fi.Length % ret == 0) {
                ret = (uint) (fi.Length / ret);
                if(ret > SizeSmallBlocks)
                    throw new ArgumentException("File is to big!");
                if(blocks == 0 || blocks > ret)
                    return ret;
                return blocks;
            }
            throw new ArgumentException("Filesize is not dividable by block size netheir raw nor logical!");
        }

        public override string ToString() {
            return string.Format("0x{0:X08}", Config);
        }
    }
}