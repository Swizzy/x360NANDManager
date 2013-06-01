namespace x360NANDManager {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows.Forms;
    using x360NANDManager.Properties;

    public abstract class Utils {
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

        internal static void RemoveDuplicatesInList<T>(ref List<T> list) {
            var newlist = new List<T>();
            foreach(var tmp in list) {
                if(!newlist.Contains(tmp))
                    newlist.Add(tmp);
            }
            list = newlist;
        }

        //public bool CompareByteArrays(byte[] a1, byte[] a2) {
        //    if(a1 == a2)
        //        return true;
        //    if(a1 == null || a2 == null || a1.Length != a2.Length)
        //        return false;
        //    for(var i = 0; i < a1.Length; i++) {
        //        if(a1[i] != a2[i])
        //            return false;
        //    }
        //    return true;
        //}

        internal static bool CompareByteArrays(byte[] src, byte[] target, int offset) {
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

        public long GetTotalFreeSpace(string path) {
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.IsReady && drive.RootDirectory.FullName.Equals(Path.GetPathRoot(path), StringComparison.CurrentCultureIgnoreCase))
                    return drive.TotalFreeSpace;
            }
            return -1;
        }

        public bool FileSystemHas4GBSupport(string path) {
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.IsReady && drive.RootDirectory.FullName.Equals(Path.GetPathRoot(path), StringComparison.CurrentCultureIgnoreCase))
                    return !drive.DriveFormat.StartsWith("FAT", StringComparison.CurrentCultureIgnoreCase);
            }
            return false;
        }

        internal static BinaryReader OpenReader(string file) {
            try {
                return new BinaryReader(File.Open(file, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read));
            }
            catch(Exception) {
                if(MessageBox.Show(string.Format(Resources.OpenReadFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error) == DialogResult.Yes)
                    return OpenReader(file);
            }
            return null;
        }

        internal static BinaryWriter OpenWriter(string file) {
            try {
                return new BinaryWriter(File.Open(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
            }
            catch(Exception) {
                if(MessageBox.Show(string.Format(Resources.OpenWriteFailed, file, Environment.NewLine), Resources.LoadFileErrorTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error) == DialogResult.Yes)
                    return OpenWriter(file);
            }
            return null;
        }
    }
}