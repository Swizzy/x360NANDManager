namespace x360NANDManager {
    using System;

    internal static class Utils {
        public static string GetSizeReadable(long i) {
            if(i >= 0x1000000000000000) // Exabyte
                return string.Format("{0:0.##} EB", (double) (i >> 50) / 1024);
            if(i >= 0x4000000000000) // Petabyte
                return string.Format("{0:0.##} PB", (double) (i >> 40) / 1024);
            if(i >= 0x10000000000) // Terabyte
                return string.Format("{0:0.##} TB", (double) (i >> 30) / 1024);
            if(i >= 0x40000000) // Gigabyte
                return string.Format("{0:0.##} GB", (double) (i >> 20) / 1024);
            if(i >= 0x100000) // Megabyte
                return string.Format("{0:0.##} MB", (double) (i >> 10) / 1024);
            return i >= 0x400 ? string.Format("{0:0.##} KB", (double) i / 1024) : string.Format("{0} B", i);
        }

        public static double GetPercentage(long current, long max) {
            return ((double) current / max) * 100;
        }

        public static byte[] Correctendian(byte[] array) {
            if(BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return array;
        }
    }
}