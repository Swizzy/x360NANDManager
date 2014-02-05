namespace x360NANDManager
{
    using System;

    public interface IFlasherOutput
    {
        event EventHandler<EventArg<string>> Status;
        event EventHandler<EventArg<string>> Error;
        event EventHandler<EventArg<ProgressData>> Progress;

        void UpdateProgress(uint current, uint max, uint totalCurrent = 0, uint total = 0);

        void UpdateMMCProgress(long currentSector, long lastSector, int sectorSize, long bufsize);

        void UpdateMMCProgressEX(long offset, long maximum, long bufsize);

        void UpdateStatus(string message, params object[] args);

        void SendError(string message, params object[] args);

        bool IsBadBlock(uint status, uint block, string operation, bool verbose = false);

    }
}
