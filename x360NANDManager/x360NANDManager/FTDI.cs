namespace x360NANDManager {
    internal class FTDI {
        public static byte[] FlashRead(uint block) {
            return new byte[0];
        }

        public static void FlashErase(uint block) {
        }

        public static void FlashWrite(uint block, byte[] data) {
        }

        public static uint Status {
            get { return 0; }
            set { }
        }
    }
}