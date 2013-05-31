namespace x360NANDManagerGUI {
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Windows.Forms;

    internal sealed partial class Debug : Form {
        internal Debug() {
            InitializeComponent();
        }

        public void AddDebug(string msg) {
            try {
                if(!InvokeRequired) {
                    outputbox.AppendText(msg + Environment.NewLine);
                    outputbox.Select(outputbox.Text.Length, 0);
                    outputbox.ScrollToCaret();
                }
                else
                    Invoke(new MethodInvoker(() => AddDebug(msg)));
            }
            catch(Exception) {
            }
        }

        private void SaveToolStripMenuItemClick(object sender, EventArgs e) {
            var sfd = new SaveFileDialog {
                                         FileName = "debug.log"
                                         };
            if(sfd.ShowDialog() == DialogResult.OK)
                File.WriteAllLines(sfd.FileName, outputbox.Lines);
        }

        private void ClearToolStripMenuItemClick(object sender, EventArgs e) {
            outputbox.Clear();
        }

        private void ContextMenuStrip1Opening(object sender, CancelEventArgs e) {
            e.Cancel = outputbox.Text.Length == 0;
        }
    }
}