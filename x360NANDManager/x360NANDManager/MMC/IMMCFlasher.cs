namespace x360NANDManager.MMC
{
    using System;
    using System.Collections.Generic;

    public interface IMMCFlasher
    {
        void Init();

        void Release();

        void Reset();

        void ZeroData(long offset, long length, int verboseLevel);

        void Write(long offset, long length, byte[] data, MMCWriteModes mode = MMCWriteModes.None, int verboseLevel = 0);

        void Write(long offset, long length, string file, MMCWriteModes mode = MMCWriteModes.None, int verboseLevel = 0);

        void Read(long offset, long length, out byte[] data, int verboseLevel = 0);

        void Read(long offset, long length, string file, int verboseLevel = 0);

        void Read(long offset, long length, List<string> files, int verboseLevel = 0);

        List<MMCDevice> GetDevices();
    }

    [Flags]
    public enum MMCWriteModes
    {
        None = 0,
        EraseFirst = 1,
        VerifyAfter = 2
    }

}
