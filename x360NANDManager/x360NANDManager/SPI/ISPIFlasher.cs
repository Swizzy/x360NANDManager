namespace x360NANDManager.SPI {
    using System;
    using System.Collections.Generic;

    public interface ISPIFlasher : IFlasherOutput {
        void Init(out XConfig config);

        void DeInit();

        void Release();

        void Reset();

        void Abort();

        #region Erase

        void EraseBlock(uint blockID, int verboseLevel = 0);

        void Erase(uint startBlock, uint blockCount, int verboseLevel = 0);

        #endregion Erase

        #region Write

        void WriteBlock(uint blockID, byte[] data, int verboseLevel = 0);

        void Write(uint startBlock, uint blockCount, byte[] data, SPIWriteModes modes = SPIWriteModes.None, int verboseLevel = 0);

        void Write(uint startBlock, uint blockCount, string file, SPIWriteModes modes = SPIWriteModes.None, int verboseLevel = 0);

        #endregion Write

        #region Read

        void ReadBlock(uint blockID, out byte[] data, int verboseLevel = 0);

        void Read(uint startBlock, uint blockCount, out byte[] data, int verboseLevel = 0);

        void Read(uint startBlock, uint blockCount, string file, int verboseLevel = 0);

        void Read(uint startBlock, uint blockCount, IEnumerable<string> files, int verboseLevel = 0);

        #endregion Read
    }

    [Flags] public enum SPIWriteModes {
        None = 0,
        RAW = None,
        AddSpare = 1,
        CorrectSpare = 2,
        EraseFirst = 4,
        VerifyAfter = 8
    }
}