namespace x360NANDManager {
    using System;
    using System.IO;

    public static class MetaUtils {
        internal static uint GetMetaTypeFromFile(ref BinaryReader fileStream) {
            var pos = fileStream.BaseStream.Position;

            #region Block 0

            fileStream.BaseStream.Seek(0x200, SeekOrigin.Begin); // Seek to first page spare
            var rawData = fileStream.ReadBytes(0x10);
            var tmp = GetMetaData(rawData);
            fileStream.BaseStream.Seek(pos, SeekOrigin.Begin); // Restore it to the original state
            if(Utils.CompareByteArrays(BlockUtils.UnInitializedSpareBuffer, ref rawData, 0))
                throw new NotSupportedException();
            if(GetBadBlockMarker(ref tmp, 2) == 0xFF && GetLBA(ref tmp, 2) == 0) // This will ONLY be valid for BigBlocks and is the easiest way to check if it's that one ;) (we don't have to care about badblocks then, as this will be false for the others)
                return 2;

            #endregion

            #region Block 1

            fileStream.BaseStream.Seek(0x4400, SeekOrigin.Begin); // Seek to first page spare of block 1
            tmp = GetMetaData(fileStream.ReadBytes(0x10));
            fileStream.BaseStream.Seek(pos, SeekOrigin.Begin); // Restore it to the original state
            if(GetBadBlockMarker(ref tmp, 0) == 0xFF) {
                // Check if block is marked bad, if it isn't, then try to tell if it's MetaType 0 or MetaType 1
                if(GetLBARaw0(ref tmp) == 1)
                    return 0;
                if(GetLBARaw1(ref tmp) == 1)
                    return 1;
            }

            #endregion

            #region Last Block

            fileStream.BaseStream.Seek(0x4000, SeekOrigin.End); // Seek to first page spare of block the last block
            tmp = GetMetaData(fileStream.ReadBytes(0x10));
            fileStream.BaseStream.Seek(pos, SeekOrigin.Begin); // Restore it to the original state
            if(GetBadBlockMarker(ref tmp, 0) == 0xFF) {
                if(GetLBARaw0(ref tmp) == 1)
                    return 0;
                if(GetLBARaw1(ref tmp) == 1)
                    return 1;
            }

            #endregion

            throw new NotSupportedException("Sorry, this NAND appears to be invalid! :'(");
        }

        public static uint GetMetaTypeFromBuffer(ref byte[] data) {
            #region Block 0

            var rawData = new byte[0x10];
            Buffer.BlockCopy(data, 0x200, rawData, 0, rawData.Length); // Get first page of block 0
            var tmp = GetMetaData(rawData);
            if(Utils.CompareByteArrays(BlockUtils.UnInitializedSpareBuffer, ref rawData, 0))
                throw new NotSupportedException();
            if(GetBadBlockMarker(ref tmp, 2) == 0xFF && GetLBA(ref tmp, 2) == 0) // This will ONLY be valid for BigBlocks and is the easiest way to check if it's that one ;) (we don't have to care about badblocks then, as this will be false for the others)
                return 2;

            #endregion

            #region Block 1

            tmp = GetMetaData(ref data, 0x21); // Get first page of block 1
            if(GetBadBlockMarker(ref tmp, 0) == 0xFF) {
                // Check if block is marked bad, if it isn't, then try to tell if it's MetaType 0 or MetaType 1
                if(GetLBARaw0(ref tmp) == 1)
                    return 0;
                if(GetLBARaw1(ref tmp) == 1)
                    return 1;
            }

            #endregion

            #region Last Block

            tmp = GetMetaData(ref data, (uint) ((data.Length - 0x4200) / 0x210)); // Get first page of block last block
            if(GetBadBlockMarker(ref tmp, 0) == 0xFF) {
                if(GetLBARaw0(ref tmp) == 1)
                    return 0;
                if(GetLBARaw1(ref tmp) == 1)
                    return 1;
            }

            #endregion

            throw new NotSupportedException("Sorry, this NAND appears to be invalid! :'(");
        }

        public static MetaData GetMetaData(ref byte[] data, uint page) {
            if(data.Length % 0x210 != 0)
                throw new ArgumentException("data must be a multipile of 0x210 bytes!");
            var offset = (int) (page * 0x210);
            if(offset + 0x210 > data.Length)
                throw new ArgumentOutOfRangeException("page", @"Page * 0x210 + 0x210 must be within data!");
            var tmp = new byte[0x10];
            Buffer.BlockCopy(data, offset + 0x200, tmp, 0, tmp.Length);
            return new MetaData(tmp);
        }

        public static MetaData GetMetaData(ref byte[] pageData) {
            if(pageData.Length != 0x210)
                throw new ArgumentException("pageData must be 0x210 bytes!");
            var tmp = new Byte[0x10];
            Buffer.BlockCopy(pageData, 0x200, tmp, 0, tmp.Length);
            return new MetaData(tmp);
        }

        public static MetaData GetMetaData(byte[] pageSpare) {
            if(pageSpare.Length != 0x10)
                throw new ArgumentException("pageSpare must be 0x10 bytes!");
            return new MetaData(pageSpare);
        }

        public static UInt16 GetLBA(ref MetaData data, uint metaType) {
            byte id0, id1;
            switch(metaType) {
                case 0:
                    id0 = data.Meta0.BlockID0;
                    id1 = data.Meta0.BlockID1;
                    break;
                case 1:
                    id0 = data.Meta1.BlockID0;
                    id1 = data.Meta1.BlockID1;
                    break;
                case 2:
                    id0 = data.Meta2.BlockID0;
                    id1 = data.Meta2.BlockID1;
                    break;
                default:
                    throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
            }
            return (ushort) (id0 << 8 | id1);
        }

        private static UInt16 GetLBARaw0(ref MetaData data) { return (ushort) ((data.RawData[1] & 0xF) << 8 | data.RawData[0]); }

        private static UInt16 GetLBARaw1(ref MetaData data) { return (ushort) ((data.RawData[2] & 0xF) << 8 | data.RawData[1]); }

        //public static void SetLBA(ref MetaData data, uint metaType, UInt16 lba) {
        //    var id0 = (byte) ((lba >> 8) & 0xFF);
        //    var id1 = (byte) (lba & 0xFF);
        //    switch(metaType) {
        //        case 0:
        //            data.Meta0.BlockID0 = id0;
        //            data.Meta0.BlockID1 = id1;
        //            break;
        //        case 1:
        //            data.Meta1.BlockID0 = id0;
        //            data.Meta1.BlockID1 = id1;
        //            break;
        //        case 2:
        //            data.Meta2.BlockID0 = id0;
        //            data.Meta2.BlockID1 = id1;
        //            break;
        //        default:
        //            throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
        //    }
        //}

        public static byte GetBlockType(ref MetaData data, uint metaType) {
            switch(metaType) {
                case 0:
                    return data.Meta0.FsBlockType;
                case 1:
                    return data.Meta1.FsBlockType;
                case 2:
                    return data.Meta2.FsBlockType;
                default:
                    throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
            }
        }

        //public static void SetBlockType(ref MetaData data, uint metaType, byte blockType) {
        //    switch(metaType) {
        //        case 0:
        //            data.Meta0.FsBlockType = blockType;
        //            break;
        //        case 1:
        //            data.Meta1.FsBlockType = blockType;
        //            break;
        //        case 2:
        //            data.Meta2.FsBlockType = blockType;
        //            break;
        //        default:
        //            throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
        //    }
        //}

        public static byte GetBadBlockMarker(ref MetaData data, uint metaType) {
            switch(metaType) {
                case 0:
                    return data.Meta0.BadBlock;
                case 1:
                    return data.Meta1.BadBlock;
                case 2:
                    return data.Meta2.BadBlock;
                default:
                    throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
            }
        }

        //public static void SetBadBlockMarker(ref MetaData data, uint metaType, byte marker) {
        //    switch(metaType) {
        //        case 0:
        //            data.Meta0.BadBlock = marker;
        //            break;
        //        case 1:
        //            data.Meta1.BadBlock = marker;
        //            break;
        //        case 2:
        //            data.Meta2.BadBlock = marker;
        //            break;
        //        default:
        //            throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
        //    }
        //}

        public static UInt16 GetFSSize(ref MetaData data, uint metaType) {
            byte fs0, fs1;
            switch(metaType) {
                case 0:
                    fs0 = data.Meta0.FsSize0;
                    fs1 = data.Meta0.FsSize1;
                    break;
                case 1:
                    fs0 = data.Meta1.FsSize0;
                    fs1 = data.Meta1.FsSize1;
                    break;
                case 2:
                    fs0 = data.Meta2.FsSize0;
                    fs1 = data.Meta2.FsSize1;
                    break;
                default:
                    throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
            }
            return (ushort) (fs0 << 8 | fs1);
        }

        //public static void SetFSSize(ref MetaData data, uint metaType, UInt16 fsSize) {
        //    var fs0 = (byte) ((fsSize >> 8) & 0xFF);
        //    var fs1 = (byte) (fsSize & 0xFF);
        //    switch(metaType) {
        //        case 0:
        //            data.Meta0.FsSize0 = fs0;
        //            data.Meta0.FsSize1 = fs1;
        //            break;
        //        case 1:
        //            data.Meta1.FsSize0 = fs0;
        //            data.Meta1.FsSize1 = fs1;
        //            break;
        //        case 2:
        //            data.Meta2.FsSize0 = fs0;
        //            data.Meta2.FsSize1 = fs1;
        //            break;
        //        default:
        //            throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
        //    }
        //}

        public static UInt16 GetFsFreePages(ref MetaData data, uint metaType) {
            switch(metaType) {
                case 0:
                    return data.Meta0.FsPageCount;
                case 1:
                    return data.Meta1.FsPageCount;
                case 2:
                    return (ushort) (data.Meta2.FsPageCount * 4);
                default:
                    throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
            }
        }

        //public static void SetFsFreePages(ref MetaData data, uint metaType, UInt16 pageCount, bool divideIt = true) {
        //    switch(metaType) {
        //        case 0:
        //            data.Meta0.FsPageCount = (byte) (pageCount & 0xFF);
        //            break;
        //        case 1:
        //            data.Meta1.FsPageCount = (byte) (pageCount & 0xFF);
        //            break;
        //        case 2:
        //            if(divideIt)
        //                data.Meta2.FsPageCount = (byte) ((pageCount * 4) & 0xFF);
        //            else
        //                data.Meta2.FsPageCount = (byte) (pageCount & 0xFF);
        //            break;
        //        default:
        //            throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
        //    }
        //}

        public static UInt32 GetFsSequence(ref MetaData data, uint metaType) {
            byte seq0, seq1, seq2, seq3;
            switch(metaType) {
                case 0:
                    seq0 = data.Meta0.FsSequence0;
                    seq1 = data.Meta0.FsSequence1;
                    seq2 = data.Meta0.FsSequence2;
                    seq3 = data.Meta0.FsSequence3;
                    break;
                case 1:
                    seq0 = data.Meta1.FsSequence0;
                    seq1 = data.Meta1.FsSequence1;
                    seq2 = data.Meta1.FsSequence2;
                    seq3 = data.Meta1.FsSequence3;
                    break;
                case 2:
                    seq0 = data.Meta2.FsSequence0;
                    seq1 = data.Meta2.FsSequence1;
                    seq2 = data.Meta2.FsSequence2;
                    seq3 = 0;
                    break;
                default:
                    throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
            }
            return (uint) (seq3 << 24 | seq2 << 16 | seq1 << 8 | seq0);
        }

        //public static void SetFsSequence(ref MetaData data, uint metaType, UInt32 fsSequence) {
        //    var seq0 = (byte) (fsSequence & 0xFF);
        //    var seq1 = (byte) ((fsSequence >> 8) & 0xFF);
        //    var seq2 = (byte) ((fsSequence >> 16) & 0xFF);
        //    var seq3 = (byte) ((fsSequence >> 24) & 0xFF);
        //    switch(metaType) {
        //        case 0:
        //            data.Meta0.FsSequence0 = seq0;
        //            data.Meta0.FsSequence1 = seq1;
        //            data.Meta0.FsSequence2 = seq2;
        //            data.Meta0.FsSequence3 = seq3;
        //            break;
        //        case 1:
        //            data.Meta1.FsSequence0 = seq0;
        //            data.Meta1.FsSequence1 = seq1;
        //            data.Meta1.FsSequence2 = seq2;
        //            data.Meta1.FsSequence3 = seq3;
        //            break;
        //        case 2:
        //            data.Meta2.FsSequence0 = seq0;
        //            data.Meta2.FsSequence1 = seq1;
        //            data.Meta2.FsSequence2 = seq2;
        //            break;
        //        default:
        //            throw new NotSupportedException(string.Format("metaType: {0} is currently not supported!", metaType));
        //    }
        //}

        #region Nested type: MetaData

        public sealed class MetaData {
            internal readonly MetaType0 Meta0;
            internal readonly MetaType1 Meta1;
            internal readonly MetaType2 Meta2;

            internal MetaData(byte[] rawData, uint metaType = 0) {
                Meta0 = new MetaType0(ref rawData);
                Meta1 = new MetaType1(ref rawData);
                Meta2 = new MetaType2(ref rawData);
                MetaType = metaType;
                RawData = rawData;
            }

            internal readonly byte[] RawData;

            internal uint MetaType { get; private set; }
        }

        #endregion

        #region Nested type: MetaType0

        internal sealed class MetaType0 {
            private byte[] _data;

            public MetaType0(ref byte[] rawData) { _data = rawData; }

            public byte FsBlockType {
                get { return (byte) (_data[12] & 0x3F); }
            }

            public byte FsPageCount {
                get { return _data[9]; }
            } // free pages left in block (ie: if 3 pages are used by cert then this would be 29:0x1d)

            public byte FsSequence0 {
                get { return _data[2]; }
            }

            public byte FsSequence1 {
                get { return _data[3]; }
            }

            public byte FsSequence2 {
                get { return _data[4]; }
            }

            public byte FsSequence3 {
                get { return _data[6]; }
            }

            public byte FsSize0 {
                get { return _data[8]; }
            }

            public byte FsSize1 {
                get { return _data[7]; }
            } // ((FsSize0<<8)+FsSize1) = cert size

            public byte BlockID0 {
                get { return (byte) (_data[2] >> 4); }
            }

            public byte BlockID1 {
                get { return _data[1]; }
            }

            public byte BadBlock {
                get { return _data[5]; }
            }
        }

        #endregion

        #region Nested type: MetaType1

        internal sealed class MetaType1 {
            private byte[] _data;

            public MetaType1(ref byte[] rawData) { _data = rawData; }

            public byte FsBlockType {
                get { return (byte) (_data[12] & 0x3F); }
            }

            public byte FsPageCount {
                get { return _data[9]; }
            } // free pages left in block (ie: if 3 pages are used by cert then this would be 29:0x1d)

            public byte FsSequence0 {
                get { return _data[0]; }
            }

            public byte FsSequence1 {
                get { return _data[3]; }
            }

            public byte FsSequence2 {
                get { return _data[4]; }
            }

            public byte FsSequence3 {
                get { return _data[6]; }
            }

            public byte FsSize0 {
                get { return _data[8]; }
            }

            public byte FsSize1 {
                get { return _data[7]; }
            } // ((FsSize0<<8)+FsSize1) = cert size

            public byte BlockID0 {
                get { return (byte) (_data[1] >> 4); }
            }

            public byte BlockID1 {
                get { return _data[0]; }
            }

            public byte BadBlock {
                get { return _data[5]; }
            }
        }

        #endregion

        #region Nested type: MetaType2

        internal sealed class MetaType2 {
            public byte BadBlock { get { return _data[0]; } }
            public byte BlockID0 { get { return (byte)(_data[2] >> 4); } }
            public byte BlockID1 { get { return _data[1]; } }
            public byte FsBlockType { get { return (byte)(_data[12] & 0x3F); } }
            public byte FsPageCount { get { return _data[9]; } } // FS: 04 (system config reserve) free pages left in block (multiples of 4 pages, ie if 3f then 3f*4 pages are free after)
            public byte FsSequence0 { get { return _data[5]; } }
            public byte FsSequence1 { get { return _data[4]; } }
            public byte FsSequence2 { get { return _data[3]; } }
            public byte FsSize0 { get { return _data[8]; } } // FS: 20 (size of flash filesys in smallblocks >>5)
            public byte FsSize1 { get { return _data[7]; } } //FS: 06 (system reserve block number) else ((FsSize0<<16)+(FsSize1<<8)) = cert size
            private byte[] _data;

            public MetaType2(ref byte[] rawData) { _data = rawData; }
        }

        #endregion
    }
}