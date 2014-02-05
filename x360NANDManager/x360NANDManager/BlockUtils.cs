namespace x360NANDManager {
    using System;

    public abstract class BlockUtils : Utils {
        private static readonly byte[] UnInitializedSpareBuffer = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        private static byte[] CalcECD(ref byte[] data, int offset) {
            uint val = 0;
            uint tmp = 0;
            var count = 0;
            for(uint i = 0; i < 0x1066; i++) {
                if((i & 31) == 0) {
                    tmp = ~BitConverter.ToUInt32(data, (count + offset));
                    count += 4;
                }
                val ^= tmp & 1;
                tmp >>= 1;
                if((val & 1) != 0)
                    val ^= 0x6954559;
                val >>= 1;
            }
            val = ~val;
            return new[] {
                (byte) (val << 6),
                (byte) ((val >> 2) & 0xFF),
                (byte) ((val >> 10) & 0xFF),
                (byte) ((val >> 18) & 0xFF)
            };
        }

        //public void AddSpare(ref byte[] data, short metaType = 0x0) {
        //    var dataOffset = 0x0;
        //    var offset = 0x0;
        //    var data2 = new byte[(data.Length / 0x200) * 0x210];
        //    for(var page = 0x0; page < data.Length / 0x200; page++, offset += 0x210, dataOffset += 0x200) {
        //        Buffer.BlockCopy(data, dataOffset, data2, offset, 0x200);
        //        byte[] tmp;
        //        switch(metaType) {
        //            case 0x1:
        //            case 0x0:
        //                tmp = BitConverter.GetBytes((ushort) (page / 0x20));
        //                tmp = Correctendian(tmp);
        //                if(metaType == 0x0)
        //                    Buffer.BlockCopy(tmp, 0x0, data2, offset + 0x200, tmp.Length);
        //                else
        //                    Buffer.BlockCopy(tmp, 0x0, data2, offset + 0x201, tmp.Length);
        //                data2[offset + 0x205] = 0xFF;
        //                break;
        //            case 0x2:
        //                tmp = BitConverter.GetBytes((ushort) (page / 0x100));
        //                tmp = Correctendian(tmp);
        //                Buffer.BlockCopy(tmp, 0x0, data2, offset + 0x201, tmp.Length);
        //                data2[offset + 0x200] = 0xFF;
        //                break;
        //            default:
        //                return;
        //        }
        //        tmp = CalcECD(ref data2, offset);
        //        Buffer.BlockCopy(tmp, 0, data2, offset + 0x20C, tmp.Length);
        //    }
        //    data = data2;
        //}

        protected static byte[] AddSpareBlock(ref byte[] data, uint block, uint metaType = 0x0) {
            var ret = new byte[0x4200];
            var dataoffset = 0;
            var page = block * 0x20;
            for(var offset = 0; offset < ret.Length; offset += 0x210, dataoffset += 0x200, page++) {
                Buffer.BlockCopy(data, dataoffset, ret, offset, 0x200);
                byte[] tmp;
                switch(metaType) {
                    case 0x0:
                    case 0x1:
                        tmp = BitConverter.GetBytes((ushort) (page / 0x20));
                        tmp = Correctendian(tmp);
                        if(metaType == 0x0)
                            Buffer.BlockCopy(tmp, 0x0, ret, offset + 0x200, tmp.Length);
                        else
                            Buffer.BlockCopy(tmp, 0x0, ret, offset + 0x201, tmp.Length);
                        ret[offset + 0x205] = 0xFF;
                        break;
                    case 0x2:
                        tmp = BitConverter.GetBytes((ushort) (page / 0x100));
                        tmp = Correctendian(tmp);
                        Buffer.BlockCopy(tmp, 0x0, ret, offset + 0x201, tmp.Length);
                        ret[offset + 0x200] = 0xFF;
                        break;
                    default:
                        return new byte[0x0];
                }
                tmp = CalcECD(ref ret, offset);
                Buffer.BlockCopy(tmp, 0x0, ret, offset + 0x20C, tmp.Length);
            }
            return ret;
        }

        private static void ReInitSpareBlock(ref byte[] data, uint block, uint metaType = 0x0) {
            var page = block * 0x20;
            for(var offset = 0; offset < (0x4200); offset += 0x210, page++) {
                byte[] tmp;
                Buffer.BlockCopy(new byte[0x10], 0, data, 0x200 + offset, 0x10); // Set the data to all 0's so we don't mess up by accident...
                switch(metaType) {
                    case 0x0:
                    case 0x1:
                        tmp = BitConverter.GetBytes((ushort) (page / 0x20));
                        tmp = Correctendian(tmp);
                        if(metaType == 0x0)
                            Buffer.BlockCopy(tmp, 0x0, data, offset + 0x200, tmp.Length);
                        else
                            Buffer.BlockCopy(tmp, 0x0, data, offset + 0x201, tmp.Length);
                        data[offset + 0x205] = 0xFF;
                        break;
                    case 0x2:
                        tmp = BitConverter.GetBytes((ushort) (page / 0x100));
                        tmp = Correctendian(tmp);
                        Buffer.BlockCopy(tmp, 0x0, data, offset + 0x201, tmp.Length);
                        data[offset + 0x200] = 0xFF;
                        break;
                    default:
                        throw new NotSupportedException("MetaType is not supported...");
                }
                tmp = CalcECD(ref data, offset);
                Buffer.BlockCopy(tmp, 0x0, data, offset + 0x20C, tmp.Length);
            }
        }

        //public void CorrectSpare(ref byte[] data, short metaType = 0x0) {
        //    for(uint block = 0; block <= (data.Length / 0x210) / 0x20; block++)
        //        CorrectSpareBlock(ref data, block, metaType);
        //}

        protected static void CorrectSpareBlock(ref byte[] data, uint block, uint metaType = 0x0) {
            var offset = 0;
            for (var page = block * 0x20; page < (block * 0x20) + 0x20; page++, offset += 0x210)
            {
                if(CompareByteArrays(UnInitializedSpareBuffer, ref data, offset + 0x200)) {
                    // Check if the page we are about to process is initalized, if not... re-initalize the whole block
                    ReInitSpareBlock(ref data, block, metaType); // Assume that the whole block is not initalized, and re-initalize it...
                    return; // We don't want to continue processing it as we've allready processed this block...
                }
                byte[] tmp;
                var skip = block != 0;
                switch(metaType) {
                    case 1:
                    case 0:
                        tmp = BitConverter.GetBytes((ushort) (page / 0x20));
                        tmp = Correctendian(tmp);
                        if(metaType == 0) {
                            for(var i = 0; i < tmp.Length; i++) {
                                if(data[i + offset + 0x200] != tmp[i])
                                    skip = false;
                            }
                            if(skip)
                                break;
                            Buffer.BlockCopy(tmp, 0, data, offset + 0x200, tmp.Length);
                        }
                        else {
                            for(var i = 0; i < tmp.Length; i++) {
                                if(data[i + offset + 0x201] != tmp[i])
                                    skip = false;
                            }
                            if(skip)
                                break;
                            data[offset + 0x200] = 0x00;
                            Buffer.BlockCopy(tmp, 0, data, offset + 0x201, tmp.Length);
                        }
                        data[offset + 0x205] = 0xFF;
                        break;
                    case 2:
                        tmp = BitConverter.GetBytes((ushort) (page / 0x100));
                        tmp = Correctendian(tmp);
                        for(var i = 0; i < tmp.Length; i++) {
                            if(data[i + offset + 0x201] != tmp[i])
                                skip = false;
                        }
                        if(skip)
                            break;
                        Buffer.BlockCopy(tmp, 0, data, offset + 0x201, tmp.Length);
                        data[offset + 0x200] = 0xFF;
                        data[offset + 0x205] = 0x00;
                        break;
                    default:
                        throw new NotSupportedException("MetaType is not supported");
                }
                tmp = CalcECD(ref data, offset);
                Buffer.BlockCopy(tmp, 0, data, offset + 0x20C, tmp.Length);
            }
        }
    }
}