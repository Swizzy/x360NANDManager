namespace x360NANDManager {
    using System;

    internal class BlockUtils {
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
            return new[] { (byte) (val << 6), (byte) ((val >> 2) & 0xFF), (byte) ((val >> 10) & 0xFF), (byte) ((val >> 18) & 0xFF) };
        }

        public static void AddSpare(ref byte[] data, short metaType = 0) {
            var dataOffset = 0;
            var offset = 0;
            var data2 = new byte[(data.Length / 512) * 528];
            for(var page = 0; page < data.Length / 512; page++) {
                Buffer.BlockCopy(data, dataOffset, data2, offset, 0x200);
                byte[] tmp;
                switch(metaType) {
                    case 1:
                    case 0:
                        tmp = BitConverter.GetBytes((ushort) (page / 32));
                        tmp = Utils.Correctendian(tmp);
                        if(metaType == 0)
                            Buffer.BlockCopy(tmp, 0, data2, offset + 0x200, tmp.Length);
                        else
                            Buffer.BlockCopy(tmp, 0, data2, offset + 0x201, tmp.Length);
                        data2[offset + 0x205] = 0xFF;
                        break;
                    case 2:
                        tmp = BitConverter.GetBytes((ushort) (page / 256));
                        tmp = Utils.Correctendian(tmp);
                        Buffer.BlockCopy(tmp, 0, data2, offset + 0x201, tmp.Length);
                        data2[offset + 0x200] = 0xFF;
                        break;
                    default:
                        return;
                }
                tmp = CalcECD(ref data2, offset);
                Buffer.BlockCopy(tmp, 0, data2, offset + 0x20C, tmp.Length);
                offset += 0x210;
                dataOffset += 0x200;
            }
            data = data2;
        }

        public static void CorrectSpare(ref byte[] data, short metaType = 0) {
            for(uint block = 0; block < (data.Length / 0x210) / 32; block++) {
                CorrectSpareBlock(ref data, block, metaType);
            }
        }

        public static void AddSpareBlock(ref byte[] data, uint block, short metaType = 0) {

        }

        public static void CorrectSpareBlock(ref byte[] data, uint block, short metaType = 0) {
            for(var page = block * 32; page < (block * 32) + 32; page++) {
                var offset = (int)(page * 0x210);
                byte[] tmp;
                var skip = true;
                switch (metaType)
                {
                    case 1:
                    case 0:
                        tmp = BitConverter.GetBytes((ushort)(page / 32));
                        tmp = Utils.Correctendian(tmp);
                        if (metaType == 0)
                        {
                            for (var i = 0; i < tmp.Length; i++)
                                if (data[i + offset + 0x200] != tmp[i])
                                    skip = false;
                            if (skip)
                                break;
                            Buffer.BlockCopy(tmp, 0, data, offset + 0x200, tmp.Length);
                        }
                        else
                        {
                            for (var i = 0; i < tmp.Length; i++)
                                if (data[i + offset + 0x201] != tmp[i])
                                    skip = false;
                            if (skip)
                                break;
                            data[offset + 0x200] = 0x00;
                            Buffer.BlockCopy(tmp, 0, data, offset + 0x201, tmp.Length);
                        }
                        data[offset + 0x205] = 0xFF;
                        break;
                    case 2:
                        tmp = BitConverter.GetBytes((ushort)(page / 256));
                        tmp = Utils.Correctendian(tmp);
                        for (var i = 0; i < tmp.Length; i++)
                            if (data[i + offset + 0x201] != tmp[i])
                                skip = false;
                        if (skip)
                            break;
                        Buffer.BlockCopy(tmp, 0, data, offset + 0x201, tmp.Length);
                        data[offset + 0x200] = 0xFF;
                        data[offset + 0x205] = 0x00;
                        break;
                    default:
                        return;
                }
                tmp = CalcECD(ref data, offset);
                Buffer.BlockCopy(tmp, 0, data, offset + 0x20C, tmp.Length);
            }
        }
    }
}