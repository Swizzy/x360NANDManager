namespace x360NANDManager
{
    using System;

    public interface IFlasherOutput
    {
        event EventHandler<EventArg<string>> Status;
        event EventHandler<EventArg<string>> Error;
        event EventHandler<EventArg<ProgressData>> Progress;

        void UpdateProgress(uint current, uint max, uint total = 0);

        void UpdateStatus(string message);

        void SendError(string message);

        bool IsBadBlock(uint status, uint block, string operation, bool verbose = false);

    }
}
