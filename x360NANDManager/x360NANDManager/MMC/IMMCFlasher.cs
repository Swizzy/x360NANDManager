namespace x360NANDManager.MMC
{
    using System.Collections.Generic;

    public interface IMMCFlasher : IFlasherOutput
    {
        void Release();

        void Reset(bool read = true);

        void Abort();

        void ZeroData(long startSector = 0, long sectorCount = 0);

        void ZeroDataEX(long offset = 0, long length = 0);

        void Write(byte[] data, long startSector = 0, long sectorCount = 0, bool verify = true);

        void Write(string file, long startSector = 0, long sectorCount = 0, bool verify = true);

        void WriteEX(byte[] data, long offset = 0, long length = 0, bool verify = true);

        void WriteEX(string file, long offset = 0, long length = 0, bool verify = true);

        byte[] Read(long startSector = 0, long sectorCount = 0);

        void Read(string file, long startSector = 0, long sectorCount = 0);

        void Read(IEnumerable<string> files, long startSector = 0, long sectorCount = 0);

        byte[] ReadEX(long offset = 0, long length = 0);

        void ReadEX(string file, long offset = 0, long length = 0);

        void ReadEX(IEnumerable<string> files, long offset = 0, long length = 0);
    }
}
