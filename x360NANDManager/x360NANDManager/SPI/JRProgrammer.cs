namespace x360NANDManager.SPI {
    internal sealed class JRProgrammer : ARMBase {
        internal JRProgrammer() {
            Initialized = DeviceInit(0x11D4, 0x8338);
        }

        public bool FlashErase(uint block, bool verboseError = false) {
            if(base.FlashErase(block)) {
                GetARMStatus();
                SendCMD(Commands.DataStatus, block, 0x4);
                Status = GetARMStatus();
                Utils.IsBadBlock(Status, block, "Erasing", verboseError);
                return true;
            }
            return false;
        }

        public bool FlashWrite(uint block, byte[] buf, bool verboseError = false) {
            if(base.FlashWrite(block, buf)) {
                GetFlashStatus();
                Utils.IsBadBlock(Status, block, "Writing", verboseError);
                return true;
            }
            return false;
        }
    }
}