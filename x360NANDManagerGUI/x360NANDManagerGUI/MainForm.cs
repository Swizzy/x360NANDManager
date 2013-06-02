namespace x360NANDManagerGUI {
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;
    using x360NANDManager;
    using x360NANDManager.SPI;

    internal sealed partial class MainForm : Form {
        private readonly Debug _dbg = new Debug();
        private bool _abort;
        private Stopwatch _sw = new Stopwatch();
        private ISPIFlasher _xNANDManagerSPI;

        public MainForm() {
            InitializeComponent();
        }

        private void Form1Load(object sender, EventArgs e) {
            Main.Debug += MainOnDebug;
            dllversionlabel.Text = Main.Version;
        }

        private void MainOnDebug(object sender, EventArg<string> e) {
            try {
                _dbg.AddDebug(e.Data);
            }
            catch(Exception) {
            }
        }

        private void SetAppState(bool busy) {
            optionsbox.Enabled = !busy;
            abortbtn.Enabled = busy;
            readbtn.Enabled = !busy;
            erasebtn.Enabled = !busy;
            writebtn.Enabled = !busy;
        }

        private void MainOnProgress(object sender, EventArg<ProgressData> e) {
            SetProgress((int) e.Data.Percentage);
        }

        private void SetProgress(int value) {
            try {
                if(!InvokeRequired)
                    progress.Value = value;
                else
                    Invoke(new MethodInvoker(() => SetProgress(value)));
            }
            catch {
            }
        }

        private void MainOnError(object sender, EventArg<string> e) {
            if(e.Data == null)
                return;
            SetText(e.Data);
        }

        private void SetText(string text) {
            try {
                if(!InvokeRequired) {
                    outputbox.AppendText(string.Format("{0}{1}", text, Environment.NewLine));
                    outputbox.Select(outputbox.Text.Length, 0);
                    outputbox.ScrollToCaret();
                }
                else
                    Invoke(new MethodInvoker(() => SetText(text)));
            }
            catch {
            }
        }

        private void BackgroundWorker1DoWork(object sender, DoWorkEventArgs e) {
            _sw = Stopwatch.StartNew();
            if(!(e.Argument is BWArgs))
                return;
            var args = e.Argument as BWArgs;
            try {
                if(args.Device == BWArgs.Devices.MMC)
                    throw new NotImplementedException();
                _xNANDManagerSPI = Main.GetSPIFlasher();
                if(_xNANDManagerSPI == null) {
                    e.Result = false;
                    return;
                }
                _xNANDManagerSPI.Error += MainOnError;
                _xNANDManagerSPI.Status += MainOnError;
                _xNANDManagerSPI.Progress += MainOnProgress;
                switch(args.Operation) {
                    case BWArgs.Operations.Read:
                        e.Result = true;
                        _xNANDManagerSPI.Read((uint) spiblockbox.Value, (uint) spicountbox.Value, args.File, 1);
                        break;
                    case BWArgs.Operations.Erase:
                        e.Result = true;
                        _xNANDManagerSPI.Erase((uint) spiblockbox.Value, (uint) spicountbox.Value, 1);
                        break;
                    case BWArgs.Operations.Write:
                        e.Result = true;
                        var mode = SPIWriteModes.None;
                        if (args.AddSpare)
                            mode |= SPIWriteModes.AddSpare;
                        else if (args.CorrectSpare)
                            mode |= SPIWriteModes.CorrectSpare;
                        if (args.EraseFirst)
                            mode |= SPIWriteModes.EraseFirst;
                        if (args.Verify)
                            mode |= SPIWriteModes.VerifyAfter;
                        _xNANDManagerSPI.Write((uint) spiblockbox.Value, (uint) spicountbox.Value, args.File, mode, 1);
                        break;
                    default:
                        throw new Exception("Unkown Operation");
                }
            }
            finally {
                if(args.Device == BWArgs.Devices.SPI) {
                    if(_xNANDManagerSPI != null) {
                        _xNANDManagerSPI.Error -= MainOnError;
                        _xNANDManagerSPI.Status -= MainOnError;
                        _xNANDManagerSPI.Progress -= MainOnProgress;
                    }
                    _xNANDManagerSPI = null;
                }
            }
        }

        private void ReadbtnClick(object sender, EventArgs e) {
            var sfd = new SaveFileDialog {
                                         FileName = "flashdmp.bin", Title = "Select where to save the dump..."
                                         };
            if(sfd.ShowDialog() != DialogResult.OK)
                return;
            outputbox.Text = "";
            var args = new BWArgs(BWArgs.Operations.Read) {
                                                          File = sfd.FileName
                                                          };
            bw.RunWorkerAsync(args);
            SetAppState(true);
        }

        private void WritebtnClick(object sender, EventArgs e) {
            var ofd = new OpenFileDialog {
                                         FileName = "updflash.bin", Title = "Select where to save the dump..."
                                         };
            if(ofd.ShowDialog() != DialogResult.OK)
                return;
            outputbox.Text = "";
            var args = new BWArgs(BWArgs.Operations.Write) {
                                                           File = ofd.FileName
                                                           };
            bw.RunWorkerAsync(args);
            SetAppState(true);
        }

        private void ErasebtnClick(object sender, EventArgs e) {
            if(MessageBox.Show("Are you sure you want to erase?! you'll lose EVERYTHING!\nIf you do intend to erase Click on NO", "Are you sure?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.No)
                return;
            outputbox.Text = "";
            var args = new BWArgs(BWArgs.Operations.Erase);
            bw.RunWorkerAsync(args);
            SetAppState(true);
        }

        private void BWRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            _sw.Stop();
            _dbg.AddDebug(string.Format("Completed after {0:F0} Minutes {1:F0} Seconds", _sw.Elapsed.TotalMinutes, _sw.Elapsed.Seconds));
            SetAppState(false);
            var res = e.Result is bool && (bool) e.Result;
            if(res && !_abort)
                MessageBox.Show("Operation completed successfully!", "Done!");
            else if(!_abort)
                MessageBox.Show("Operation completed with errors!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show("Operation aborted", "Aborted");
        }

        private void ClearLogClick(object sender, EventArgs e) {
            outputbox.Clear();
        }

        private void SaveLogClick(object sender, EventArgs e) {
            var sfd = new SaveFileDialog {
                                         FileName = "x360NANDManager.log", Title = "Select where to save the log", DefaultExt = "log", AddExtension = true, Filter = "Log files|*.log|Text Files|*.txt|All Files|*.*"
                                         };
            if(sfd.ShowDialog() == DialogResult.OK)
                File.WriteAllLines(sfd.FileName, outputbox.Lines);
        }

        private void LogmenuOpening(object sender, CancelEventArgs e) {
            e.Cancel = outputbox.Text.Length == 0;
        }

        private void DeviceCheckedChanged(object sender, EventArgs e) {
            mmcoffsetbox.Enabled = mmc.Checked;
            mmccountbox.Enabled = mmc.Checked;
            mmcdevice.Enabled = mmc.Checked;
            spiblockbox.Enabled = !mmc.Checked;
            spicountbox.Enabled = !mmc.Checked;
        }

        private void AbortbtnClick(object sender, EventArgs e) {
            _xNANDManagerSPI.Abort();
            _abort = true;
        }

        private void HclinkClick(object sender, EventArgs e) {
            Process.Start("http://www.homebrew-connection.org");
        }

        #region Nested type: BWArgs

        internal sealed class BWArgs {
            public readonly bool AddSpare;
            public readonly bool CorrectSpare;
            public readonly Devices Device;
            public readonly bool EraseFirst;
            public readonly Operations Operation;
            public readonly bool Verify;
            public string File;

            public BWArgs(Operations op) {
                Operation = op;
                foreach(var ctrl in Program.MainForm.devices.Controls) {
                    if(!(ctrl is RadioButton))
                        continue;
                    var rb = ctrl as RadioButton;
                    if(rb.Checked) {
                        Device = (Devices) Enum.Parse(typeof(Devices), rb.Name, true);
                        break;
                    }
                    Device = Devices.None;
                }
                Verify = Program.MainForm.verifyBox.Checked;
                EraseFirst = Program.MainForm.eraseBox.Checked;
                AddSpare = Program.MainForm.addSpareBox.Checked;
                CorrectSpare = Program.MainForm.correctSpareBox.Checked;
            }

            #region Nested type: Devices

            internal enum Devices {
                None,
                SPI,
                MMC
            }

            #endregion

            #region Nested type: Operations

            internal enum Operations {
                Read,
                Erase,
                Write
            }

            #endregion
        }

        #endregion
    }
}