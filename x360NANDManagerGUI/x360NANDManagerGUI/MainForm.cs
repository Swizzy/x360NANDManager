namespace x360NANDManagerGUI {
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;
    using x360NANDManager;
    using x360NANDManager.MMC;
    using x360NANDManager.SPI;
    using x360NANDManager.XSVF;
    using x360NANDManagerGUI.Properties;

    internal sealed partial class MainForm : Form {
        private readonly Debug _dbg = new Debug();
        private bool _abort;
        private readonly bool _isAdmin;
        private Stopwatch _sw = new Stopwatch();
        private ISPIFlasher _spiFlasher;
        private IMMCFlasher _mmcFlasher;
        private IXSVFFlasher _xsvfFlasher;

        internal static string GetSizeReadable(long i)
        {
            if (i >= 0x1000000000000000) // Exabyte
                return string.Format("{0:0.##} EB", (double)(i >> 50) / 1024);
            if (i >= 0x4000000000000) // Petabyte
                return string.Format("{0:0.##} PB", (double)(i >> 40) / 1024);
            if (i >= 0x10000000000) // Terabyte
                return string.Format("{0:0.##} TB", (double)(i >> 30) / 1024);
            if (i >= 0x40000000) // Gigabyte
                return string.Format("{0:0.##} GB", (double)(i >> 20) / 1024);
            if (i >= 0x100000) // Megabyte
                return string.Format("{0:0.##} MB", (double)(i >> 10) / 1024);
            return i >= 0x400 ? string.Format("{0:0.##} KB", (double)i / 1024) : string.Format("{0} B", i);
        }

        public MainForm() {
            InitializeComponent();
            _isAdmin = Program.IsUserAnAdmin();
            if(!_isAdmin)
                MessageBox.Show(Resources.MMCDisabledNeedAdmin, Resources.AdminRequiredForMMC, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            mmc.Enabled = _isAdmin;

        }

        private void Form1Load(object sender, EventArgs e) {
            Main.Debug += MainOnDebug;
            dllversionlabel.Text = Main.Version.Replace("x360NANDManager", "DLL");
            _dbg.ShowDebug();
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
            xsvfbtn.Enabled = !busy && !mmc.Checked;

        }

        private void MainOnProgress(object sender, EventArg<ProgressData> e) {
            SetProgress(e.Data);

        }

        private void SetProgress(ProgressData data) {
            try {
                if(!InvokeRequired) {
                    progress.Value = (int) data.Percentage;
                    if(spi.Checked && _xsvfFlasher == null) {
                        if(data.Current > data.Maximum) {
                            if(data.Current - data.Maximum > data.Maximum)
                                data.Current = data.Current - data.Maximum - data.Maximum;
                            else
                                data.Current = data.Current - data.Maximum;
                        }
                        statuslbl.Text = string.Format("Processed block 0x{0:X} of 0x{1:X}", data.Current, data.Maximum);
                    }
                    else
                        statuslbl.Text = string.Format("Processed {0} of {1} ({2}/s)", GetSizeReadable(data.Current), GetSizeReadable(data.Maximum), GetSizeReadable((long) (data.Current / _sw.Elapsed.TotalSeconds)));
                }
                else
                    Invoke(new MethodInvoker(() => SetProgress(data)));
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
            _abort = false;
            _sw = Stopwatch.StartNew();
            if(!(e.Argument is BWArgs))
                return;
            var args = e.Argument as BWArgs;
            try
            {
                #region XSVF

                if (args.Operation == BWArgs.Operations.XSVF) {
                    try {
                        _xsvfFlasher = Main.GetXSVFFlasher();
                        _xsvfFlasher.Error += MainOnError;
                        _xsvfFlasher.Status += MainOnError;
                        _xsvfFlasher.Progress += MainOnProgress;
                        _xsvfFlasher.WriteXSVF(args.File);
                        e.Result = true;
                    }
                    catch(X360NANDManagerException ex) {
                        if(ex.ErrorLevel == X360NANDManagerException.ErrorLevels.IncompatibleDevice)
                            MessageBox.Show(Resources.IncompatibleXSVF);
                        else
                            throw;
                    }
                }

                #endregion
                else if(args.Device == BWArgs.Devices.MMC) {
                    if (args.MMCDevice == null)
                        throw new Exception("Something went horribly wrong here!!");
                    _mmcFlasher = Main.GetMMCFlasher(args.MMCDevice);
                    if (_mmcFlasher == null) {
                        e.Result = false;
                        return;
                    }
                    _mmcFlasher.Error += MainOnError;
                    _mmcFlasher.Status += MainOnError;
                    _mmcFlasher.Progress += MainOnProgress;
                    e.Result = true;
                    switch(args.Operation) {
                        case BWArgs.Operations.Read:
                            _dbg.AddDebug("Reading started...");
                            _mmcFlasher.Read(args.File, (long)mmcoffsetbox.Value, (long)mmccountbox.Value);
                            break;
                        case BWArgs.Operations.Erase:
                            _dbg.AddDebug("Erase started...");
                            _mmcFlasher.ZeroData((long)mmcoffsetbox.Value, (long)mmccountbox.Value);
                            break;
                        case BWArgs.Operations.Write:
                            _dbg.AddDebug("Write started...");
                            _mmcFlasher.Write(args.File, (long)mmcoffsetbox.Value, (long)mmccountbox.Value, args.Verify);
                            break;
                        default:
                            _dbg.AddDebug("Unkown command found!");
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else {
                    _spiFlasher = Main.GetSPIFlasher();
                    if(_spiFlasher == null) {
                        e.Result = false;
                        return;
                    }
                    _spiFlasher.Error += MainOnError;
                    _spiFlasher.Status += MainOnError;
                    _spiFlasher.Progress += MainOnProgress;
                    e.Result = true;
                    switch(args.Operation) {
                        case BWArgs.Operations.Read:
                            _spiFlasher.Read((uint) spiblockbox.Value, (uint) spicountbox.Value, args.File, 1);
                            break;
                        case BWArgs.Operations.Erase:
                            _spiFlasher.Erase((uint) spiblockbox.Value, (uint) spicountbox.Value, 1);
                            break;
                        case BWArgs.Operations.Write:
                            var mode = SPIWriteModes.None;
                            if(args.AddSpare)
                                mode |= SPIWriteModes.AddSpare;
                            else if(args.CorrectSpare)
                                mode |= SPIWriteModes.CorrectSpare;
                            if(args.EraseFirst)
                                mode |= SPIWriteModes.EraseFirst;
                            if(args.Verify)
                                mode |= SPIWriteModes.VerifyAfter;
                            _spiFlasher.Write((uint) spiblockbox.Value, (uint) spicountbox.Value, args.File, mode, 1);
                            break;
                        default:
                            throw new Exception("Unkown Operation");
                    }
                }
            }
                catch (Exception ex) {
                    MessageBox.Show(string.Format("Internal Exception: {0}{1}", Environment.NewLine, ex));
                }
            finally {
                switch(args.Device) {
                    case BWArgs.Devices.SPI:
                        if(_spiFlasher != null) {
                            _spiFlasher.Error -= MainOnError;
                            _spiFlasher.Status -= MainOnError;
                            _spiFlasher.Progress -= MainOnProgress;
                        }
                        _spiFlasher = null;
                        break;
                    case BWArgs.Devices.MMC:
                        if(_mmcFlasher != null)
                        {
                            _mmcFlasher.Error -= MainOnError;
                            _mmcFlasher.Status -= MainOnError;
                            _mmcFlasher.Progress -= MainOnProgress;
                        }
                        _mmcFlasher = null;
                        break;
                }
                if (args.Operation == BWArgs.Operations.XSVF) {
                    if (_xsvfFlasher != null) {
                        _xsvfFlasher.Error -= MainOnError;
                        _xsvfFlasher.Status -= MainOnError;
                        _xsvfFlasher.Progress -= MainOnProgress;
                    }
                    _xsvfFlasher = null;
                }
            }
        }

        private void ReadbtnClick(object sender, EventArgs e) {
            var sfd = new SaveFileDialog {
                                         FileName = "flashdmp.bin", Title = Resources.SelectDumpLocation
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
                                         FileName = "updflash.bin", Title = Resources.SelectFileToWrite
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
            if(MessageBox.Show(Resources.EraseSafetyMessage, Resources.AreYouSure, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.No)
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
                MessageBox.Show(Resources.OpSuccess, Resources.Done);
            else if(!_abort)
                MessageBox.Show(Resources.OpFailed, Resources.Done, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show(Resources.OpAborted, Resources.Aborted);
        }

        private void ClearLogClick(object sender, EventArgs e) {
            outputbox.Clear();
        }

        private void SaveLogClick(object sender, EventArgs e) {
            var sfd = new SaveFileDialog {
                                         FileName = "x360NANDManager.log", Title = Resources.SelectLogLocation, DefaultExt = "log", AddExtension = true, Filter = Resources.LogFilter
                                         };
            if(sfd.ShowDialog() == DialogResult.OK)
                File.WriteAllLines(sfd.FileName, outputbox.Lines);
        }

        private void LogmenuOpening(object sender, CancelEventArgs e) {
            e.Cancel = outputbox.Text.Length == 0;
        }

        private void DeviceCheckedChanged(object sender, EventArgs e) {
            if(mmc.Checked && sender == mmc)
                mmcdevice.DataSource = Main.GetMMCDeviceList();
            else if (sender == mmc)
                mmcdevice.DataSource = null;
            mmcoffsetbox.Enabled = mmc.Checked;
            mmccountbox.Enabled = mmc.Checked;
            mmcdevice.Enabled = mmc.Checked;
            mmclbl.Enabled = mmc.Checked;
            nandMMCStyle.Enabled = mmc.Checked;

            spiblockbox.Enabled = !mmc.Checked;
            spicountbox.Enabled = !mmc.Checked;
            rawbox.Enabled = !mmc.Checked;
            addSpareBox.Enabled = !mmc.Checked;
            correctSpareBox.Enabled = !mmc.Checked;
            eraseBox.Enabled = !mmc.Checked;
            xsvfbtn.Enabled = !mmc.Checked;
        }

        private void AbortbtnClick(object sender, EventArgs e) {
            if (_spiFlasher != null)
                _spiFlasher.Abort();
            if (_xsvfFlasher != null)
                _xsvfFlasher.Abort();
            if (_mmcFlasher != null)
                _mmcFlasher.Abort();
            _abort = true;
        }

        private void HclinkClick(object sender, EventArgs e) {
            Process.Start("http://www.homebrew-connection.org");
        }

        private void XsvfbtnClick(object sender, EventArgs e) {
            var ofd = new OpenFileDialog {
                                         FileName = "xc2c64a.xsvf", Title = Resources.SelectFileToWrite
                                         };
            if(ofd.ShowDialog() != DialogResult.OK)
                return;
            outputbox.Text = "";
            var args = new BWArgs(BWArgs.Operations.XSVF) {
                                                          File = ofd.FileName
                                                          };
            bw.RunWorkerAsync(args);
            SetAppState(true);
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
            public readonly MMCDevice MMCDevice;

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
                if(Program.MainForm.mmcdevice.SelectedItem != null) {
                    MMCDevice = Program.MainForm.mmcdevice.SelectedItem as MMCDevice;
                    if(MMCDevice != null)
                        MMCDevice.NANDMMCStyle = Program.MainForm.nandMMCStyle.Checked;
                }
                Verify = Program.MainForm.verifyBox.Checked;
                EraseFirst = Program.MainForm.eraseBox.Checked;
                AddSpare = Program.MainForm.addSpareBox.Checked;
                CorrectSpare = Program.MainForm.correctSpareBox.Checked;
            }

            #region Nested type: Devices

            internal enum Devices {
                None,
                FTDI,
                SPI,
                MMC
            }

            #endregion

            #region Nested type: Operations

            internal enum Operations {
                Read,
                Erase,
                Write,
                XSVF
            }

            #endregion
        }

        #endregion

        private void MmcdeviceSelectedIndexChanged(object sender, EventArgs e)
        {
            if(mmcdevice.SelectedIndex > mmcdevice.Items.Count)
                return;
            var tmp = mmcdevice.Items[mmcdevice.SelectedIndex] as MMCDevice;
            if (tmp == null) {
                mmccountbox.Maximum = 0;
                mmcoffsetbox.Maximum = 0;
            }
            else
            {
                mmccountbox.Maximum = tmp.Size / tmp.DiskGeometryEX.Geometry.BytesPerSector;
                mmcoffsetbox.Maximum = (tmp.Size / tmp.DiskGeometryEX.Geometry.BytesPerSector) - 1;
            }
        }
    }
}