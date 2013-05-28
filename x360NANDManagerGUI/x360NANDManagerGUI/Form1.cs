using System.Windows.Forms;

namespace x360NANDManagerGUI
{
    using System;
    using x360NANDManager;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1Load(object sender, EventArgs e)
        {
            Main.Error += MainOnError;
            Main.Status += MainOnError;
            Main.Progress += MainOnProgress;
        }

        private void SetAppState(bool busy) {
            optionsbox.Enabled = !busy;
            devices.Enabled = !busy;
            readbtn.Enabled = !busy;
            erasebtn.Enabled = !busy;
            writebtn.Enabled = !busy;
        }

        private void MainOnProgress(object sender, EventArg<ProgressData> e) {
            SetProgress((int) e.Data.Percentage);
        }

        private void SetProgress(int value) {
            try {
                if (!InvokeRequired)
                    progress.Value = value;
                else
                    Invoke(new MethodInvoker(() => SetProgress(value)));
            }
            catch { }
        }

        private void MainOnError(object sender, EventArg<string> e) {
            if (e.Data == null)
                return;
            SetText(e.Data);
        }
        
        private void SetText(string text) {
            try {
                if (!InvokeRequired) {
                    outputbox.AppendText(string.Format("{0}{1}", text, Environment.NewLine));
                    outputbox.Select(outputbox.Text.Length, 0);
                    outputbox.ScrollToCaret();
                }
                else
                    Invoke(new MethodInvoker(() => SetText(text)));
            }
            catch { }
        }

        private void BackgroundWorker1DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (!(e.Argument is BWArgs))
                return;
            var args = e.Argument as BWArgs;
            if (args.Device == BWArgs.Devices.ARM) {
                switch (args.Operation) {
                    case BWArgs.Operations.Read:
                        Main.ReadARM(args.File);
                        break;
                    case BWArgs.Operations.Erase:
                        Main.EraseARM();
                        break;
                    case BWArgs.Operations.Write:
                        Main.WriteARM(args.File, 0, 0, 0, args.AddSpare, args.CorrectSpare, args.EraseFirst, args.Verify);
                        break;
                }
            }
        }

        internal class BWArgs {
            internal enum Devices {
                None,
                ARM,
                FTDI,
                MMC
            }

            internal enum Operations {
                Read,
                Erase,
                Write
            }

            public BWArgs(Operations op) {
                Operation = op;
                foreach (var ctrl in Program.MainForm.devices.Controls) {
                    if (!(ctrl is RadioButton)) continue;
                    var rb = ctrl as RadioButton;
                    if (rb.Checked) {
                        Device = (Devices) Enum.Parse(typeof (Devices), rb.Name, true);
                        break;
                    }
                    Device = Devices.None;
                }
                Verify = Program.MainForm.verifyBox.Checked;
                EraseFirst = Program.MainForm.eraseBox.Checked;
                AddSpare = Program.MainForm.addSpareBox.Checked;
                CorrectSpare = Program.MainForm.correctSpareBox.Checked;
            }

            public readonly Devices Device;
            public readonly Operations Operation;
            public string File;
            public readonly bool Verify;
            public readonly bool EraseFirst;
            public readonly bool AddSpare;
            public readonly bool CorrectSpare;
        }

        private void ReadbtnClick(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog {
                                             FileName = "flashdmp.bin",
                                             Title = "Select where to save the dump..."
                                         };
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            outputbox.Text = "";
            var args = new BWArgs(BWArgs.Operations.Read) { File = sfd.FileName };
            bw.RunWorkerAsync(args);
            SetAppState(true);
        }

        private void WritebtnClick(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog {
                FileName = "updflash.bin",
                Title = "Select where to save the dump..."
            };
            if (ofd.ShowDialog() != DialogResult.OK) 
                return;
            outputbox.Text = "";
            var args = new BWArgs(BWArgs.Operations.Write) { File = ofd.FileName };
            bw.RunWorkerAsync(args);
            SetAppState(true);
        }

        private void ErasebtnClick(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to erase?! you'll lose EVERYTHING!", "Are you sure?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            outputbox.Text = "";
            var args = new BWArgs(BWArgs.Operations.Erase);
            bw.RunWorkerAsync(args);
            SetAppState(true);
        }

        private void BWRunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            SetAppState(false);
        }
    }
}