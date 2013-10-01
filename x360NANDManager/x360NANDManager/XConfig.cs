namespace x360NANDManager {
    using System;
    using System.IO;

    public sealed class XConfigException : Exception {
        public readonly uint Config;

        internal XConfigException(uint config) {
            Config = config;
        }

        public override string ToString() {
            return string.Format("Unsupported Controller Type & Block Type Combination: 0x{0:X8}", Config);
        }
    }

    public sealed class XConfig {
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
                            throw new XConfigException(config);
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
                            throw new XConfigException(config);
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
                                throw new XConfigException(config);
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
                            throw new XConfigException(config);
                    }
                    break;
                default:
                    throw new XConfigException(config);
            }
            SizeSmallBlocks = (BlockSize * SizeBlocks) / 0x4000;
            PagesPerBlock = (BlockSize / PageSize);
            BlockRawSize = (BlockSize / PageSize) * (PageSize + MetaSize);
            SizeRawBytes = (SizeBytes / BlockSize) * BlockRawSize;
        }

        public uint BlockSize { get; private set; }
        public uint BlockRawSize { get; private set; }

        public uint BlockReadSize {
            get { return 0x4200; } // Always read 0x4200 bytes for now...
        }

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

        /// <summary>
        ///   Converts RAW Block count (PageSize (0x200) + MetaSize (0x10) * PagesPerBlock (0x20)) to Actual data size
        ///   <exception cref="ArgumentException">If Size isn't dividable by BlockRawSize</exception>
        /// </summary>
        /// <param name="blocks"> Blocks to convert to size </param>
        /// <returns> Size </returns>
        internal uint BlocksToDataSize(uint blocks) {
            return (BlockRawSize * blocks);
        }

        /// <summary>
        ///   Converts Data Size to RAW Block count (PageSize (0x200) + MetaSize (0x10) * PagesPerBlock (0x20))
        ///   <exception cref="ArgumentException">If Size isn't dividable by BlockRawSize</exception>
        /// </summary>
        /// <param name="size"> Size to convert to blocks </param>
        /// <returns> BlockCount </returns>
        internal uint SizeToRawBlocks(long size) {
            if(size % 0x4200 == 0)
                return (uint) (size / 0x4200);
            throw new ArgumentException("Size isn't dividable by RAW Block Size!");
        }

        /// <summary>
        ///   Converts Data Size to Block count (PageSize (0x200) * PagesPerBlock (0x20))
        ///   <exception cref="ArgumentException">If Size isn't dividable by BlockSize</exception>
        /// </summary>
        /// <param name="size"> Size to convert to blocks </param>
        /// <returns> BlockCount </returns>
        internal uint SizeToBlocks(long size) {
            if(size % 0x4000 == 0)
                return (uint) (size / 0x4000);
            throw new ArgumentException("Size isn't dividable by Block Size!");
        }

        /// <summary>
        ///   Make sure the block count is valid (if 0, set it to whatever the flashconfig says is right with the startblock else make sure it's within a valid range)
        /// </summary>
        /// <param name="startblock"> BlockID which the operation should start at </param>
        /// <param name="blocks"> Block Count specified by user </param>
        /// <returns> Correct block count (if 0 it'll be calculated) </returns>
        internal uint FixBlockCount(uint startblock, uint blocks) {
            if(blocks == 0)
                return SizeSmallBlocks - startblock;
            if(blocks > SizeSmallBlocks - startblock)
                throw new ArgumentException("You cannot erase/write more blocks then the device have!");
            return blocks;
        }

        /// <summary>
        ///   Gets block count from filesize and corrects user specified size if it's to big
        ///   <exception cref="ArgumentException">If File contains more data then can fit on the device or if the device size cannot be divided by block size</exception>
        /// </summary>
        /// <param name="file"> File to get data from </param>
        /// <param name="blocks"> User specified block count </param>
        /// <returns> Proper block count to use </returns>
        internal uint GetFileBlockCount(string file, uint blocks = 0) {
            var fi = new FileInfo(file);
            Main.SendDebug(string.Format("File length: 0x{0:X}", fi.Length));
            try {
                var ret = SizeToRawBlocks(fi.Length);
                Main.SendDebug(string.Format("RAW Blocks: 0x{0:X}", ret));
                if(blocks == 0 || blocks > ret)
                    return ret;
                return blocks;
            }
            catch(Exception) {
                try {
                    var ret = SizeToBlocks(fi.Length);
                    Main.SendDebug(string.Format("Blocks: 0x{0:X}", ret));
                    if(blocks == 0 || blocks > ret)
                        return ret;
                    return blocks;
                }
                catch(Exception) {
                    throw new ArgumentException("Filesize is not dividable by block size netheir raw nor logical!");
                }
            }
        }

        public override string ToString() {
            return string.Format("0x{0:X08}", Config);
        }
    }
}