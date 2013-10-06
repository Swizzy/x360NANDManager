namespace x360NANDManager {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows.Forms;
    using x360NANDManager.Properties;

    public abstract class Utils {
#if DEBUG
        private static readonly char[] HexCharTable = new[] {
                                                            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
                                                            };

        internal static string ArrayToHex(IList<byte> value, int i = 0, int count = -1)
        {
            var c = new char[value.Count * 2];
            if (count == -1)
                count = value.Count - i;
            else
                count = count + i;
            for (var p = 0; i < count; )
            {
                var d = value[i++];
                c[p++] = HexCharTable[d / 0x10];
                c[p++] = HexCharTable[d % 0x10];
            }
            var tmp = new string(c);
            var ret = "0x";
            var pos = 0;
            foreach(var s in tmp) {
                if (pos > 0) {
                    if(pos % 32 == 0)
                        ret += "\n";
                    if (pos % 2 == 0 && pos % 32 != 0)
                        ret += " 0x";
                    else
                        ret += "0x";
                }
                ret += s;
                pos++;
            }
            return ret.Trim();
        }

#endif

        internal static string GetSizeReadable(long i) {
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

        internal static byte[] Correctendian(byte[] array) {
            if(BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return array;
        }

        internal static T[] RemoveDuplicatesInList<T>(IEnumerable<T> list) {
            Main.SendDebug("Removing duplicate entries...");
            var newlist = new List<T>();
            foreach(var tmp in list) {
                if(!newlist.Contains(tmp))
                    newlist.Add(tmp);
            }
            return newlist.ToArray();
        }

        public bool CompareByteArrays(byte[] buf, byte[] buf2) {
            if(buf == buf2)
                return true;
            if(buf == null || buf2 == null || buf.Length != buf2.Length)
                return false;
            for(var i = 0; i < buf.Length; i++) {
                if(buf[i] != buf2[i])
                    return false;
            }
            return true;
        }

        internal static bool CompareByteArrays(ref byte[] buf, ref byte[] largebuf, int offset) {
            if(buf == largebuf)
                return true;
            if(buf == null || largebuf == null || buf.Length > largebuf.Length - offset)
                return false;
            for(var i = 0; i < buf.Length; i++) {
                if(buf[i] != largebuf[offset + i])
                    return false;
            }
            return true;
        }

        protected static long GetTotalFreeSpace(string path) {
            Main.SendDebug(string.Format("Getting total Free Space for: {0}", Path.GetPathRoot(path)));
            foreach(var drive in DriveInfo.GetDrives()) {
                if(!drive.IsReady || !drive.RootDirectory.FullName.Equals(Path.GetPathRoot(path), StringComparison.CurrentCultureIgnoreCase))
                    continue;
                if((drive.TotalFreeSpace < 0xFFFFFFFE || FileSystemHas4GBSupport(drive.Name)) || (drive.TotalFreeSpace < 0x7FFFFFFE || FileSystemHas2GBSupport(drive.Name)))
                    return drive.TotalFreeSpace;
                return FileSystemHas2GBSupport(drive.Name) ? 0xFFFFFFFE /* 4GB - 1 Byte */ : 0x7FFFFFFE; // 2GB - 1 Byte
            }
            throw new DirectoryNotFoundException();
        }

        private static bool FileSystemHas4GBSupport(string path) {
            Main.SendDebug("Checking 4GB+ Support...");
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.IsReady && drive.RootDirectory.FullName.Equals(Path.GetPathRoot(path), StringComparison.CurrentCultureIgnoreCase))
                    return !drive.DriveFormat.StartsWith("FAT", StringComparison.CurrentCultureIgnoreCase);
            }
            throw new DirectoryNotFoundException();
        }

        private static bool FileSystemHas2GBSupport(string path) {
            Main.SendDebug("Checking 2GB+ Support...");
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.IsReady && drive.RootDirectory.FullName.Equals(Path.GetPathRoot(path), StringComparison.CurrentCultureIgnoreCase))
                    return !drive.DriveFormat.Equals("FAT", StringComparison.CurrentCultureIgnoreCase);
            }
            throw new DirectoryNotFoundException();
        }

        internal static BinaryReader OpenReader(string file) {
            try {
                return new BinaryReader(File.Open(file, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read));
            }
            catch(Exception) {
                if(MessageBox.Show(string.Format(Resources.OpenReadFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    return OpenReader(file);
            }
            return null;
        }

        internal static BinaryWriter OpenWriter(string file) {
            try {
                return new BinaryWriter(File.Open(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
            }
            catch(Exception) {
                if(MessageBox.Show(string.Format(Resources.OpenWriteFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    return OpenWriter(file);
            }
            return null;
        }
    }
}