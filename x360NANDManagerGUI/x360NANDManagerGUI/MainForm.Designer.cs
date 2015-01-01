namespace x360NANDManagerGUI
{
    internal sealed partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.readbtn = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.outputbox = new System.Windows.Forms.RichTextBox();
            this.logmenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.erasebtn = new System.Windows.Forms.Button();
            this.optionsbox = new System.Windows.Forms.GroupBox();
            this.zeroFillBadBlocks = new System.Windows.Forms.CheckBox();
            this.nandMMCStyle = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.spiblockbox = new System.Windows.Forms.NumericUpDown();
            this.mmccountbox = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.dumpcount = new System.Windows.Forms.NumericUpDown();
            this.mmcoffsetbox = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.spicountbox = new System.Windows.Forms.NumericUpDown();
            this.devices = new System.Windows.Forms.GroupBox();
            this.mmc = new System.Windows.Forms.RadioButton();
            this.spi = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.rawbox = new System.Windows.Forms.RadioButton();
            this.verifyBox = new System.Windows.Forms.CheckBox();
            this.eraseBox = new System.Windows.Forms.CheckBox();
            this.mmclbl = new System.Windows.Forms.Label();
            this.presetsBox = new System.Windows.Forms.ComboBox();
            this.mmcdevice = new System.Windows.Forms.ComboBox();
            this.correctSpareBox = new System.Windows.Forms.RadioButton();
            this.addSpareBox = new System.Windows.Forms.RadioButton();
            this.writebtn = new System.Windows.Forms.Button();
            this.bw = new System.ComponentModel.BackgroundWorker();
            this.abortbtn = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.dllversionlabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.hclink = new System.Windows.Forms.ToolStripStatusLabel();
            this.xsvfbtn = new System.Windows.Forms.Button();
            this.statusStrip2 = new System.Windows.Forms.StatusStrip();
            this.statuslbl = new System.Windows.Forms.ToolStripStatusLabel();
            this.groupBox2.SuspendLayout();
            this.logmenu.SuspendLayout();
            this.optionsbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spiblockbox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mmccountbox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dumpcount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mmcoffsetbox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spicountbox)).BeginInit();
            this.devices.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.statusStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // readbtn
            // 
            this.readbtn.Location = new System.Drawing.Point(17, 329);
            this.readbtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.readbtn.Name = "readbtn";
            this.readbtn.Size = new System.Drawing.Size(194, 35);
            this.readbtn.TabIndex = 1;
            this.readbtn.Text = "Read";
            this.readbtn.UseVisualStyleBackColor = true;
            this.readbtn.Click += new System.EventHandler(this.ReadbtnClick);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.outputbox);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(18, 463);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Size = new System.Drawing.Size(634, 545);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "output";
            // 
            // outputbox
            // 
            this.outputbox.AcceptsTab = true;
            this.outputbox.BackColor = System.Drawing.Color.Black;
            this.outputbox.ContextMenuStrip = this.logmenu;
            this.outputbox.Cursor = System.Windows.Forms.Cursors.Default;
            this.outputbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputbox.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputbox.ForeColor = System.Drawing.Color.Lime;
            this.outputbox.Location = new System.Drawing.Point(4, 24);
            this.outputbox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.outputbox.Name = "outputbox";
            this.outputbox.ReadOnly = true;
            this.outputbox.Size = new System.Drawing.Size(626, 516);
            this.outputbox.TabIndex = 3;
            this.outputbox.Text = "";
            this.outputbox.WordWrap = false;
            // 
            // logmenu
            // 
            this.logmenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.logmenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.logmenu.Name = "contextMenuStrip1";
            this.logmenu.Size = new System.Drawing.Size(124, 64);
            this.logmenu.Opening += new System.ComponentModel.CancelEventHandler(this.LogmenuOpening);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(123, 30);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.ClearLogClick);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(123, 30);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveLogClick);
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(17, 418);
            this.progress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(634, 35);
            this.progress.TabIndex = 0;
            // 
            // erasebtn
            // 
            this.erasebtn.Location = new System.Drawing.Point(422, 329);
            this.erasebtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.erasebtn.Name = "erasebtn";
            this.erasebtn.Size = new System.Drawing.Size(230, 35);
            this.erasebtn.TabIndex = 1;
            this.erasebtn.Text = "Erase";
            this.erasebtn.UseVisualStyleBackColor = true;
            this.erasebtn.Click += new System.EventHandler(this.ErasebtnClick);
            // 
            // optionsbox
            // 
            this.optionsbox.Controls.Add(this.zeroFillBadBlocks);
            this.optionsbox.Controls.Add(this.nandMMCStyle);
            this.optionsbox.Controls.Add(this.label3);
            this.optionsbox.Controls.Add(this.spiblockbox);
            this.optionsbox.Controls.Add(this.mmccountbox);
            this.optionsbox.Controls.Add(this.label6);
            this.optionsbox.Controls.Add(this.dumpcount);
            this.optionsbox.Controls.Add(this.mmcoffsetbox);
            this.optionsbox.Controls.Add(this.label1);
            this.optionsbox.Controls.Add(this.label2);
            this.optionsbox.Controls.Add(this.label4);
            this.optionsbox.Controls.Add(this.spicountbox);
            this.optionsbox.Controls.Add(this.devices);
            this.optionsbox.Controls.Add(this.label5);
            this.optionsbox.Controls.Add(this.rawbox);
            this.optionsbox.Controls.Add(this.verifyBox);
            this.optionsbox.Controls.Add(this.eraseBox);
            this.optionsbox.Controls.Add(this.mmclbl);
            this.optionsbox.Controls.Add(this.presetsBox);
            this.optionsbox.Controls.Add(this.mmcdevice);
            this.optionsbox.Controls.Add(this.correctSpareBox);
            this.optionsbox.Controls.Add(this.addSpareBox);
            this.optionsbox.Location = new System.Drawing.Point(18, 18);
            this.optionsbox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.optionsbox.Name = "optionsbox";
            this.optionsbox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.optionsbox.Size = new System.Drawing.Size(634, 301);
            this.optionsbox.TabIndex = 3;
            this.optionsbox.TabStop = false;
            this.optionsbox.Text = "Options";
            // 
            // zeroFillBadBlocks
            // 
            this.zeroFillBadBlocks.AutoSize = true;
            this.zeroFillBadBlocks.Location = new System.Drawing.Point(440, 103);
            this.zeroFillBadBlocks.Name = "zeroFillBadBlocks";
            this.zeroFillBadBlocks.Size = new System.Drawing.Size(152, 24);
            this.zeroFillBadBlocks.TabIndex = 12;
            this.zeroFillBadBlocks.Text = "Clear BadBlocks";
            this.zeroFillBadBlocks.UseVisualStyleBackColor = true;
            // 
            // nandMMCStyle
            // 
            this.nandMMCStyle.AutoSize = true;
            this.nandMMCStyle.Enabled = false;
            this.nandMMCStyle.Location = new System.Drawing.Point(440, 32);
            this.nandMMCStyle.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nandMMCStyle.Name = "nandMMCStyle";
            this.nandMMCStyle.Size = new System.Drawing.Size(147, 24);
            this.nandMMCStyle.TabIndex = 11;
            this.nandMMCStyle.Text = "nandMMC Style";
            this.nandMMCStyle.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 263);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(91, 20);
            this.label3.TabIndex = 0;
            this.label3.Text = "Start Block:";
            // 
            // spiblockbox
            // 
            this.spiblockbox.Hexadecimal = true;
            this.spiblockbox.Location = new System.Drawing.Point(129, 260);
            this.spiblockbox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.spiblockbox.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.spiblockbox.Name = "spiblockbox";
            this.spiblockbox.Size = new System.Drawing.Size(86, 26);
            this.spiblockbox.TabIndex = 1;
            // 
            // mmccountbox
            // 
            this.mmccountbox.Enabled = false;
            this.mmccountbox.Hexadecimal = true;
            this.mmccountbox.Location = new System.Drawing.Point(129, 220);
            this.mmccountbox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mmccountbox.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.mmccountbox.Name = "mmccountbox";
            this.mmccountbox.Size = new System.Drawing.Size(297, 26);
            this.mmccountbox.TabIndex = 1;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 223);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(107, 20);
            this.label6.TabIndex = 0;
            this.label6.Text = "Sector Count:";
            // 
            // dumpcount
            // 
            this.dumpcount.Enabled = false;
            this.dumpcount.Location = new System.Drawing.Point(136, 65);
            this.dumpcount.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dumpcount.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.dumpcount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.dumpcount.Name = "dumpcount";
            this.dumpcount.Size = new System.Drawing.Size(64, 26);
            this.dumpcount.TabIndex = 10;
            this.dumpcount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // mmcoffsetbox
            // 
            this.mmcoffsetbox.Enabled = false;
            this.mmcoffsetbox.Hexadecimal = true;
            this.mmcoffsetbox.Location = new System.Drawing.Point(129, 180);
            this.mmcoffsetbox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mmcoffsetbox.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.mmcoffsetbox.Name = "mmcoffsetbox";
            this.mmcoffsetbox.Size = new System.Drawing.Size(297, 26);
            this.mmcoffsetbox.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 104);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 20);
            this.label1.TabIndex = 9;
            this.label1.Text = "Presets:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Enabled = false;
            this.label2.Location = new System.Drawing.Point(24, 68);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 20);
            this.label2.TabIndex = 9;
            this.label2.Text = "Dump Count:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(21, 183);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(99, 20);
            this.label4.TabIndex = 0;
            this.label4.Text = "Start Sector:";
            // 
            // spicountbox
            // 
            this.spicountbox.Hexadecimal = true;
            this.spicountbox.Location = new System.Drawing.Point(335, 260);
            this.spicountbox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.spicountbox.Maximum = new decimal(new int[] {
            32768,
            0,
            0,
            0});
            this.spicountbox.Name = "spicountbox";
            this.spicountbox.Size = new System.Drawing.Size(92, 26);
            this.spicountbox.TabIndex = 1;
            // 
            // devices
            // 
            this.devices.Controls.Add(this.mmc);
            this.devices.Controls.Add(this.spi);
            this.devices.Location = new System.Drawing.Point(435, 180);
            this.devices.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.devices.Name = "devices";
            this.devices.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.devices.Size = new System.Drawing.Size(194, 111);
            this.devices.TabIndex = 6;
            this.devices.TabStop = false;
            this.devices.Text = "Device";
            // 
            // mmc
            // 
            this.mmc.AutoSize = true;
            this.mmc.Location = new System.Drawing.Point(9, 65);
            this.mmc.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mmc.Name = "mmc";
            this.mmc.Size = new System.Drawing.Size(174, 24);
            this.mmc.TabIndex = 0;
            this.mmc.Text = "MMC (4GB Corona)";
            this.mmc.UseVisualStyleBackColor = true;
            this.mmc.CheckedChanged += new System.EventHandler(this.DeviceCheckedChanged);
            // 
            // spi
            // 
            this.spi.AutoSize = true;
            this.spi.Checked = true;
            this.spi.Location = new System.Drawing.Point(9, 29);
            this.spi.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.spi.Name = "spi";
            this.spi.Size = new System.Drawing.Size(140, 24);
            this.spi.TabIndex = 0;
            this.spi.TabStop = true;
            this.spi.Text = "SPI (All others)";
            this.spi.UseVisualStyleBackColor = true;
            this.spi.CheckedChanged += new System.EventHandler(this.DeviceCheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(223, 263);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 20);
            this.label5.TabIndex = 0;
            this.label5.Text = "Block Count:";
            // 
            // rawbox
            // 
            this.rawbox.AutoSize = true;
            this.rawbox.Checked = true;
            this.rawbox.Location = new System.Drawing.Point(9, 29);
            this.rawbox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rawbox.Name = "rawbox";
            this.rawbox.Size = new System.Drawing.Size(113, 24);
            this.rawbox.TabIndex = 5;
            this.rawbox.TabStop = true;
            this.rawbox.Text = "RAW Write";
            this.rawbox.UseVisualStyleBackColor = true;
            // 
            // verifyBox
            // 
            this.verifyBox.AutoSize = true;
            this.verifyBox.Location = new System.Drawing.Point(440, 66);
            this.verifyBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.verifyBox.Name = "verifyBox";
            this.verifyBox.Size = new System.Drawing.Size(155, 24);
            this.verifyBox.TabIndex = 2;
            this.verifyBox.Text = "Verify After Write";
            this.verifyBox.UseVisualStyleBackColor = true;
            // 
            // eraseBox
            // 
            this.eraseBox.AutoSize = true;
            this.eraseBox.Location = new System.Drawing.Point(258, 66);
            this.eraseBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.eraseBox.Name = "eraseBox";
            this.eraseBox.Size = new System.Drawing.Size(170, 24);
            this.eraseBox.TabIndex = 2;
            this.eraseBox.Text = "Erase Before Write";
            this.eraseBox.UseVisualStyleBackColor = true;
            // 
            // mmclbl
            // 
            this.mmclbl.AutoSize = true;
            this.mmclbl.Enabled = false;
            this.mmclbl.Location = new System.Drawing.Point(12, 143);
            this.mmclbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mmclbl.Name = "mmclbl";
            this.mmclbl.Size = new System.Drawing.Size(50, 20);
            this.mmclbl.TabIndex = 4;
            this.mmclbl.Text = "MMC:";
            // 
            // presetsBox
            // 
            this.presetsBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.presetsBox.FormattingEnabled = true;
            this.presetsBox.Location = new System.Drawing.Point(80, 101);
            this.presetsBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.presetsBox.Name = "presetsBox";
            this.presetsBox.Size = new System.Drawing.Size(348, 28);
            this.presetsBox.TabIndex = 3;
            this.presetsBox.SelectedIndexChanged += new System.EventHandler(this.presetsBox_SelectedIndexChanged);
            // 
            // mmcdevice
            // 
            this.mmcdevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.mmcdevice.Enabled = false;
            this.mmcdevice.FormattingEnabled = true;
            this.mmcdevice.Location = new System.Drawing.Point(80, 139);
            this.mmcdevice.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mmcdevice.Name = "mmcdevice";
            this.mmcdevice.Size = new System.Drawing.Size(546, 28);
            this.mmcdevice.TabIndex = 3;
            this.mmcdevice.SelectedIndexChanged += new System.EventHandler(this.MmcdeviceSelectedIndexChanged);
            // 
            // correctSpareBox
            // 
            this.correctSpareBox.AutoSize = true;
            this.correctSpareBox.Location = new System.Drawing.Point(258, 31);
            this.correctSpareBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.correctSpareBox.Name = "correctSpareBox";
            this.correctSpareBox.Size = new System.Drawing.Size(133, 24);
            this.correctSpareBox.TabIndex = 1;
            this.correctSpareBox.Text = "Correct Spare";
            this.correctSpareBox.UseVisualStyleBackColor = true;
            // 
            // addSpareBox
            // 
            this.addSpareBox.AutoSize = true;
            this.addSpareBox.Location = new System.Drawing.Point(136, 29);
            this.addSpareBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.addSpareBox.Name = "addSpareBox";
            this.addSpareBox.Size = new System.Drawing.Size(110, 24);
            this.addSpareBox.TabIndex = 0;
            this.addSpareBox.Text = "Add Spare";
            this.addSpareBox.UseVisualStyleBackColor = true;
            // 
            // writebtn
            // 
            this.writebtn.Location = new System.Drawing.Point(219, 329);
            this.writebtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.writebtn.Name = "writebtn";
            this.writebtn.Size = new System.Drawing.Size(194, 35);
            this.writebtn.TabIndex = 1;
            this.writebtn.Text = "Write";
            this.writebtn.UseVisualStyleBackColor = true;
            this.writebtn.Click += new System.EventHandler(this.WritebtnClick);
            // 
            // bw
            // 
            this.bw.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorker1DoWork);
            this.bw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BWRunWorkerCompleted);
            // 
            // abortbtn
            // 
            this.abortbtn.Enabled = false;
            this.abortbtn.Location = new System.Drawing.Point(17, 373);
            this.abortbtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.abortbtn.Name = "abortbtn";
            this.abortbtn.Size = new System.Drawing.Size(396, 35);
            this.abortbtn.TabIndex = 1;
            this.abortbtn.Text = "Cancel/Abort";
            this.abortbtn.UseVisualStyleBackColor = true;
            this.abortbtn.Click += new System.EventHandler(this.AbortbtnClick);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dllversionlabel,
            this.hclink});
            this.statusStrip1.Location = new System.Drawing.Point(0, 1050);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 21, 0);
            this.statusStrip1.Size = new System.Drawing.Size(670, 30);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // dllversionlabel
            // 
            this.dllversionlabel.Name = "dllversionlabel";
            this.dllversionlabel.Size = new System.Drawing.Size(105, 25);
            this.dllversionlabel.Text = "DLL Version";
            // 
            // hclink
            // 
            this.hclink.Name = "hclink";
            this.hclink.Size = new System.Drawing.Size(542, 25);
            this.hclink.Spring = true;
            this.hclink.Text = "www.homebrew-connection.org";
            this.hclink.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.hclink.Click += new System.EventHandler(this.HclinkClick);
            // 
            // xsvfbtn
            // 
            this.xsvfbtn.Location = new System.Drawing.Point(422, 373);
            this.xsvfbtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.xsvfbtn.Name = "xsvfbtn";
            this.xsvfbtn.Size = new System.Drawing.Size(230, 35);
            this.xsvfbtn.TabIndex = 1;
            this.xsvfbtn.Text = "Flash XSVF";
            this.xsvfbtn.UseVisualStyleBackColor = true;
            this.xsvfbtn.Click += new System.EventHandler(this.XsvfbtnClick);
            // 
            // statusStrip2
            // 
            this.statusStrip2.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statuslbl});
            this.statusStrip2.Location = new System.Drawing.Point(0, 1020);
            this.statusStrip2.Name = "statusStrip2";
            this.statusStrip2.Padding = new System.Windows.Forms.Padding(2, 0, 21, 0);
            this.statusStrip2.Size = new System.Drawing.Size(670, 30);
            this.statusStrip2.SizingGrip = false;
            this.statusStrip2.TabIndex = 5;
            this.statusStrip2.Text = "statusStrip2";
            // 
            // statuslbl
            // 
            this.statuslbl.Name = "statuslbl";
            this.statuslbl.Size = new System.Drawing.Size(81, 25);
            this.statuslbl.Text = "Progress";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(670, 1080);
            this.Controls.Add(this.statusStrip2);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.optionsbox);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.erasebtn);
            this.Controls.Add(this.writebtn);
            this.Controls.Add(this.xsvfbtn);
            this.Controls.Add(this.abortbtn);
            this.Controls.Add(this.readbtn);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainForm";
            this.Text = "x360NANDManager GUI";
            this.Load += new System.EventHandler(this.Form1Load);
            this.groupBox2.ResumeLayout(false);
            this.logmenu.ResumeLayout(false);
            this.optionsbox.ResumeLayout(false);
            this.optionsbox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spiblockbox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mmccountbox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dumpcount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mmcoffsetbox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spicountbox)).EndInit();
            this.devices.ResumeLayout(false);
            this.devices.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.statusStrip2.ResumeLayout(false);
            this.statusStrip2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button readbtn;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.RichTextBox outputbox;
        private System.Windows.Forms.Button erasebtn;
        private System.Windows.Forms.GroupBox optionsbox;
        private System.Windows.Forms.RadioButton addSpareBox;
        private System.Windows.Forms.RadioButton correctSpareBox;
        private System.Windows.Forms.CheckBox eraseBox;
        private System.Windows.Forms.CheckBox verifyBox;
        private System.Windows.Forms.Button writebtn;
        private System.ComponentModel.BackgroundWorker bw;
        private System.Windows.Forms.Label mmclbl;
        private System.Windows.Forms.ComboBox mmcdevice;
        private System.Windows.Forms.RadioButton rawbox;
        private System.Windows.Forms.GroupBox devices;
        private System.Windows.Forms.RadioButton mmc;
        private System.Windows.Forms.RadioButton spi;
        private System.Windows.Forms.NumericUpDown dumpcount;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown mmcoffsetbox;
        private System.Windows.Forms.NumericUpDown spiblockbox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown mmccountbox;
        private System.Windows.Forms.NumericUpDown spicountbox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ContextMenuStrip logmenu;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Button abortbtn;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel dllversionlabel;
        private System.Windows.Forms.ToolStripStatusLabel hclink;
        private System.Windows.Forms.Button xsvfbtn;
        private System.Windows.Forms.StatusStrip statusStrip2;
        private System.Windows.Forms.ToolStripStatusLabel statuslbl;
        private System.Windows.Forms.CheckBox nandMMCStyle;
        private System.Windows.Forms.CheckBox zeroFillBadBlocks;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox presetsBox;
    }
}

