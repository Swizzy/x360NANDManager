namespace x360NANDManager.MMC {
    public sealed class MMCDevice {
        public readonly string DisplayName;
        public readonly long Size;
        public readonly string Path;

        internal MMCDevice(string displayName, string path, long size)
        {
            DisplayName = displayName;
            Path = path;
            Size = size;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}