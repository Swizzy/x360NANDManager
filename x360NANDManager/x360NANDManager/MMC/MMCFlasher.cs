namespace x360NANDManager.MMC {
    using System;
    using System.Collections.Generic;

    internal sealed class MMCFlasher : IMMCFlasher {
        #region Implementation of IMMCFlasher

        public void Init() {
            throw new NotImplementedException();
        }

        public void Release() {
            throw new NotImplementedException();
        }

        public void Reset() {
            throw new NotImplementedException();
        }

        public void ZeroData(long offset, long length, int verboseLevel) {
            throw new NotImplementedException();
        }

        public void Write(long offset, long length, byte[] data, MMCWriteModes mode = MMCWriteModes.None, int verboseLevel = 0) {
            throw new NotImplementedException();
        }

        public void Write(long offset, long length, string file, MMCWriteModes mode = MMCWriteModes.None, int verboseLevel = 0) {
            throw new NotImplementedException();
        }

        public void Read(long offset, long length, out byte[] data, int verboseLevel = 0) {
            throw new NotImplementedException();
        }

        public void Read(long offset, long length, string file, int verboseLevel = 0) {
            throw new NotImplementedException();
        }

        public void Read(long offset, long length, List<string> files, int verboseLevel = 0) {
            throw new NotImplementedException();
        }

        public List<MMCDevice> GetDevices() {
            var ret = new List<MMCDevice>();
            
            throw new NotImplementedException();
            //return ret;
            
        }

        #endregion Implementation of IMMCFlasher
    }
}