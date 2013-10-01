namespace x360NANDManagerGUI {
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    internal static class Program {
        public static MainForm MainForm;

        [DllImport("shell32.dll", SetLastError = true)] [return : MarshalAs(UnmanagedType.Bool)] internal static extern bool IsUserAnAdmin();

        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [STAThread] private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm = new MainForm();
            Application.Run(MainForm);
        }
    }
}