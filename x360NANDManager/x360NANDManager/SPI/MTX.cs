namespace x360NANDManager.SPI {
    internal sealed class MTX : ARMBase {
        internal MTX() {
            Initialized = DeviceInit(0xffff, 0x4);
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