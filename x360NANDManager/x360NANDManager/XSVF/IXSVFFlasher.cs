namespace x360NANDManager.XSVF {
    public interface IXSVFFlasher : IFlasherOutput {
        void WriteXSVF(string file);

        void WriteXSVF(byte[] data);

        void Abort();
    }
}