namespace x360NANDManager.MMC
{
    public interface IFlasher
    {
        bool Init();

        void DeInit();

        bool Release();

        bool Reset();

        bool Erase(long offset, int verboseLevel);

        bool Write(long offset, byte[] data, int verboseLevel = 0);

        bool Read(long offset, out byte[] data, int verboseLevel = 0);
    }
}
