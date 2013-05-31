namespace x360NANDManager {
    using System;
    using System.Collections.Generic;
    using System.IO;

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

        public static void RemoveDuplicatesInList<T>(ref List<T> list) {
            var newlist = new List<T>();
            foreach(var tmp in list) {
                if(!newlist.Contains(tmp))
                    newlist.Add(tmp);
            }
            list = newlist;
        }

        public static bool CompareByteArrays(byte[] a1, byte[] a2) {
            if(a1 == a2)
                return true;
            if(a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            for(var i = 0; i < a1.Length; i++) {
                if(a1[i] != a2[i])
                    return false;
            }
            return true;
        }

        public static bool CompareByteArrays(byte[] src, byte[] target, int offset) {
            if(src == target)
                return true;
            if(src == null || target == null || src.Length > target.Length - offset)
                return false;
            for(var i = 0; i < src.Length; i++) {
                if(src[i] != target[offset + i])
                    return false;
            }
            return true;
        }

        public static long GetTotalFreeSpace(string path) {
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.IsReady && drive.RootDirectory.FullName.Equals(Path.GetPathRoot(path), StringComparison.CurrentCultureIgnoreCase))
                    return drive.TotalFreeSpace;
            }
            return -1;
        }

        public static bool FileSystemHas4GBSupport(string path) {
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.IsReady && drive.RootDirectory.FullName.Equals(Path.GetPathRoot(path), StringComparison.CurrentCultureIgnoreCase))
                    return !drive.DriveFormat.StartsWith("FAT", StringComparison.CurrentCultureIgnoreCase);
            }
            return false;
        }
        
        public static bool IsBadBlock(uint status, uint block, string operation, bool verbose = false) {
            if(status != 0x200 && status > 0) {
                Main.SendError(string.Format("ERROR: 0x{0:X} {1} block 0x{2:X}", status, operation, block));
                if(verbose) {
                    Main.SendError("This error Means:");
                    if((status & 0x800) == 0x800)
                        Main.SendError("Illegal Logical Address");
                    if((status & 0x400) == 0x400)
                        Main.SendError("NAND Not Write Protected");
                    if((status & 0x100) == 0x100)
                        Main.SendError("Interrupt");
                    if((status & 0x80) == 0x80)
                        Main.SendError("Address Alignment Error");
                    if((status & 0x40) == 0x40)
                        Main.SendError("Bad Block Marker Detected");
                    if((status & 0x20) == 0x20)
                        Main.SendError("Logical Replacement not found");
                    if((status & 0x10) == 0x10)
                        Main.SendError("ECC Error Detected");
                    else if((status & 0x8) == 0x8)
                        Main.SendError("ECC Error Detected");
                    else if((status & 0x4) == 0x4)
                        Main.SendError("ECC Error Detected");
                    if((status & 0x2) == 0x2)
                        Main.SendError("Write/Erase Error Detected");
                    if((status & 0x1) == 0x1)
                        Main.SendError("SPI Is Busy");
                }
                return true;
            }
            if(status == 0)
                Main.SendError(string.Format("Unkown Status code (0) Encounterd! while {0} block 0x{1:X}", operation, block));
            return false;
        }
    }
}