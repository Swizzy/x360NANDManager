namespace x360NANDManager
{
    using System;

    public interface IFlasherOutput
    {
        event EventHandler<EventArg<string>> Status;
        event EventHandler<EventArg<string>> Error;
        event EventHandler<EventArg<ProgressData>> Progress;
    }
}
