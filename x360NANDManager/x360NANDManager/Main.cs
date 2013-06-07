namespace x360NANDManager {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Security.Principal;
    using x360NANDManager.MMC;
    using x360NANDManager.SPI;
    using x360NANDManager.XSVF;

    public static class Main {
        private const string BaseName = "x360NANDManager v{0}.{1} (Build: {2}) {3}";
        private static readonly Version Ver = Assembly.GetExecutingAssembly().GetName().Version;

        static Main() {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
        }

        public static string Version {
            get {
#if DEBUG
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, "DEBUG BUILD");
#elif ALPHA
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, Ver.Revision > 0 ? string.Format("ALPHA{0}", Ver.Revision) : "ALPHA");
#elif BETA
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, Ver.Revision > 0 ? string.Format("BETA{0}", Ver.Revision) : "BETA");
#else
                return string.Format(BaseName, Ver.Major, Ver.Minor, Ver.Build, "");
#endif
            }
        }

        public static ISPIFlasher GetSPIFlasher() {
            if(NativeWin32.IsDeviceConnected(0xFFFF, 0x4))
                return new ARMFlasher(0xFFFF, 0x4);
            if(NativeWin32.IsDeviceConnected(0x11D4, 0x8338))
                return new ARMFlasher(0x11D4, 0x8338);
            throw new DeviceError(DeviceError.ErrorLevels.NoDeviceFound);
        }

        public static ISPIFlasher GetSPIFlasher(int vendorID, int productID) {
            if(!NativeWin32.IsDeviceConnected(vendorID, productID))
                throw new DeviceError(DeviceError.ErrorLevels.NoDeviceFound);
            return new ARMFlasher(vendorID, productID);
        }

        public static IXSVFFlasher GetXSVFFlasher() {
            if(NativeWin32.IsDeviceConnected(0xFFFF, 0x4)) {
                var flasher = new ARMXSVFFlasher(0xFFFF, 0x4);
                if(!flasher.IsCompatibleVersion())
                    throw new DeviceError(DeviceError.ErrorLevels.IncompatibleDevice);
                return flasher;
            }
            if(NativeWin32.IsDeviceConnected(0x11D4, 0x8338))
                return new JRPXSVFFlasher(0x11D4, 0x8338);
            throw new DeviceError(DeviceError.ErrorLevels.NoDeviceFound);
        }

        public static IXSVFFlasher GetXSVFFlasher(int vendorID, int productID) {
            if(NativeWin32.IsDeviceConnected(vendorID, productID))
                return new ARMXSVFFlasher(vendorID, productID);
            throw new DeviceError(DeviceError.ErrorLevels.NoDeviceFound);
        }

        [PrincipalPermissionAttribute(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static IMMCFlasher GetMMCFlasher(MMCDevice device) {
            return new MMCFlasher(device);
        }

        [PrincipalPermissionAttribute(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static IList<MMCDevice> GetMMCDeviceList(bool onlyRemoveable = true) {
            return MMCFlasher.GetDevices(onlyRemoveable);
        }

        public static event EventHandler<EventArg<string>> Debug;

        [Conditional("DEBUG")] [Conditional("ALPHA")] internal static void SendDebug(string message) {
            var dbg = Debug;
            if(dbg != null && message != null)
                dbg(null, new EventArg<string>(message));
        }

        private static Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args) {
            if(string.IsNullOrEmpty(args.Name))
                throw new Exception("DLL Read Failure (Nothing to load!)");
            var name = string.Format("{0}.dll", args.Name.Split(',')[0]);
            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("{0}.{1}", typeof(Main).Namespace, name))) {
                if(stream != null) {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return Assembly.Load(data);
                }
                throw new Exception(string.Format("Can't find external nor internal {0}!", name));
            }
        }
    }

    public struct Presets {
        #region MMCPresets enum

        public enum MMCPresets {
            SystemOnly,
            SystemOnlyEX,
            MUOnly,
            MUOnlyEX,
            Full
        }

        #endregion

        #region SPIPresets enum

        public enum SPIPresets {
            Auto,
            BigBlockSystemOnly,
            BigBlockMemoryUnit
        }

        #endregion

        public readonly long Start;
        public readonly long End;

        private Presets(MMCPresets mmc) {
            switch(mmc) {
                case MMCPresets.SystemOnly:
                    Start = 0;
                    End = 0x18000;
                    break;
                case MMCPresets.SystemOnlyEX:
                    Start = 0;
                    End = 0x3000000;
                    break;
                case MMCPresets.MUOnly:
                    Start = 0x18000;
                    End = 0;
                    break;
                case MMCPresets.MUOnlyEX:
                    Start = 0x3000000;
                    End = 0;
                    break;
                case MMCPresets.Full:
                    Start = 0;
                    End = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("mmc");
            }
        }

        private Presets(SPIPresets spi) {
            switch(spi) {
                case SPIPresets.Auto:
                    Start = 0;
                    End = 0;
                    break;
                case SPIPresets.BigBlockSystemOnly:
                    Start = 0;
                    End = 0;
                    break;
                case SPIPresets.BigBlockMemoryUnit:
                    Start = 0;
                    End = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("spi");
            }
        }
    }
}