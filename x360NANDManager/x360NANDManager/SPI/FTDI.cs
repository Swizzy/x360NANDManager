namespace x360NANDManager.SPI {
    internal class FTDI {
        public static byte[] FlashRead(uint block) {
            return new byte[0];
        }

        public static bool FlashErase(uint block, bool verboseError = false)
        {
            return false;
        }

        public static bool FlashWrite(uint block, byte[] buf, bool verboseError = false)
        {
            return false;
        }

        public static uint Status;
    }
}