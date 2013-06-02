namespace x360NANDManager {
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using x360NANDManager.MMC;
    using x360NANDManager.SPI;

    public static class Main{
        private const string BaseName = "DLL v{0}.{1} (Build: {2}) {3}";
        private static readonly Version Ver = Assembly.GetExecutingAssembly().GetName().Version;

        static Main() {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
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
            return new ARMFlasher();
        }

        public static IMMCFlasher GetMMCFlasher() {
            return  new MMCFlasher();
        }

        public static event EventHandler<EventArg<string>> Debug;

        [Conditional("DEBUG")] [Conditional("ALPHA")] [Conditional("BETA")] internal static void SendDebug(string message) {
            if(Debug != null && message != null)
                Debug(null, new EventArg<string>(message));
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
}