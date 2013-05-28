namespace x360NANDManagerGUI
{
    partial class Form1
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
            this.devices = new System.Windows.Forms.GroupBox();
            this.mmc = new System.Windows.Forms.RadioButton();
            this.ftdi = new System.Windows.Forms.RadioButton();
            this.arm = new System.Windows.Forms.RadioButton();
            this.readbtn = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.outputbox = new System.Windows.Forms.RichTextBox();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.erasebtn = new System.Windows.Forms.Button();
            this.optionsbox = new System.Windows.Forms.GroupBox();
            this.rawbox = new System.Windows.Forms.RadioButton();
            this.verifyBox = new System.Windows.Forms.CheckBox();
            this.eraseBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.mmcdevice = new System.Windows.Forms.ComboBox();
            this.correctSpareBox = new System.Windows.Forms.RadioButton();
            this.addSpareBox = new System.Windows.Forms.RadioButton();
            this.writebtn = new System.Windows.Forms.Button();
            this.bw = new System.ComponentModel.BackgroundWorker();
            this.devices.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.optionsbox.SuspendLayout();
            this.SuspendLayout();
            // 
            // devices
            // 
            this.devices.Controls.Add(this.mmc);
            this.devices.Controls.Add(this.ftdi);
            this.devices.Controls.Add(this.arm);
            this.devices.Location = new System.Drawing.Point(12, 12);
            this.devices.Name = "devices";
            this.devices.Size = new System.Drawing.Size(129, 88);
            this.devices.TabIndex = 0;
            this.devices.TabStop = false;
            this.devices.Text = "Device";
            // 
            // mmc
            // 
            this.mmc.AutoSize = true;
            this.mmc.Enabled = false;
            this.mmc.Location = new System.Drawing.Point(6, 65);
            this.mmc.Name = "mmc";
            this.mmc.Size = new System.Drawing.Size(117, 17);
            this.mmc.TabIndex = 0;
            this.mmc.TabStop = true;
            this.mmc.Text = "MMC (4GB Corona)";
            this.mmc.UseVisualStyleBackColor = true;
            // 
            // ftdi
            // 
            this.ftdi.AutoSize = true;
            this.ftdi.Enabled = false;
            this.ftdi.Location = new System.Drawing.Point(6, 42);
            this.ftdi.Name = "ftdi";
            this.ftdi.Size = new System.Drawing.Size(85, 17);
            this.ftdi.TabIndex = 0;
            this.ftdi.TabStop = true;
            this.ftdi.Text = "FTDI (Squirt)";
            this.ftdi.UseVisualStyleBackColor = true;
            // 
            // arm
            // 
            this.arm.AutoSize = true;
            this.arm.Checked = true;
            this.arm.Location = new System.Drawing.Point(6, 19);
            this.arm.Name = "arm";
            this.arm.Size = new System.Drawing.Size(105, 17);
            this.arm.TabIndex = 0;
            this.arm.TabStop = true;
            this.arm.Text = "ARM (NANDPro)";
            this.arm.UseVisualStyleBackColor = true;
            // 
            // readbtn
            // 
            this.readbtn.Location = new System.Drawing.Point(12, 106);
            this.readbtn.Name = "readbtn";
            this.readbtn.Size = new System.Drawing.Size(129, 23);
            this.readbtn.TabIndex = 1;
            this.readbtn.Text = "Read";
            this.readbtn.UseVisualStyleBackColor = true;
            this.readbtn.Click += new System.EventHandler(this.ReadbtnClick);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.outputbox);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(12, 164);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(376, 143);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "output";
            // 
            // outputbox
            // 
            this.outputbox.BackColor = System.Drawing.Color.Black;
            this.outputbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputbox.ForeColor = System.Drawing.Color.Green;
            this.outputbox.Location = new System.Drawing.Point(3, 16);
            this.outputbox.Name = "outputbox";
            this.outputbox.ReadOnly = true;
            this.outputbox.Size = new System.Drawing.Size(370, 124);
            this.outputbox.TabIndex = 3;
            this.outputbox.Text = "";
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(12, 135);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(376, 23);
            this.progress.TabIndex = 0;
            // 
            // erasebtn
            // 
            this.erasebtn.Location = new System.Drawing.Point(282, 106);
            this.erasebtn.Name = "erasebtn";
            this.erasebtn.Size = new System.Drawing.Size(106, 23);
            this.erasebtn.TabIndex = 1;
            this.erasebtn.Text = "Erase";
            this.erasebtn.UseVisualStyleBackColor = true;
            this.erasebtn.Click += new System.EventHandler(this.ErasebtnClick);
            // 
            // optionsbox
            // 
            this.optionsbox.Controls.Add(this.rawbox);
            this.optionsbox.Controls.Add(this.verifyBox);
            this.optionsbox.Controls.Add(this.eraseBox);
            this.optionsbox.Controls.Add(this.label1);
            this.optionsbox.Controls.Add(this.mmcdevice);
            this.optionsbox.Controls.Add(this.correctSpareBox);
            this.optionsbox.Controls.Add(this.addSpareBox);
            this.optionsbox.Location = new System.Drawing.Point(147, 12);
            this.optionsbox.Name = "optionsbox";
            this.optionsbox.Size = new System.Drawing.Size(241, 88);
            this.optionsbox.TabIndex = 3;
            this.optionsbox.TabStop = false;
            this.optionsbox.Text = "Options";
            // 
            // rawbox
            // 
            this.rawbox.AutoSize = true;
            this.rawbox.Checked = true;
            this.rawbox.Location = new System.Drawing.Point(102, 42);
            this.rawbox.Name = "rawbox";
            this.rawbox.Size = new System.Drawing.Size(51, 17);
            this.rawbox.TabIndex = 5;
            this.rawbox.TabStop = true;
            this.rawbox.Text = "RAW";
            this.rawbox.UseVisualStyleBackColor = true;
            // 
            // verifyBox
            // 
            this.verifyBox.AutoSize = true;
            this.verifyBox.Location = new System.Drawing.Point(183, 20);
            this.verifyBox.Name = "verifyBox";
            this.verifyBox.Size = new System.Drawing.Size(52, 17);
            this.verifyBox.TabIndex = 2;
            this.verifyBox.Text = "Verify";
            this.verifyBox.UseVisualStyleBackColor = true;
            // 
            // eraseBox
            // 
            this.eraseBox.AutoSize = true;
            this.eraseBox.Checked = true;
            this.eraseBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.eraseBox.Location = new System.Drawing.Point(102, 20);
            this.eraseBox.Name = "eraseBox";
            this.eraseBox.Size = new System.Drawing.Size(75, 17);
            this.eraseBox.TabIndex = 2;
            this.eraseBox.Text = "Erase First";
            this.eraseBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Enabled = false;
            this.label1.Location = new System.Drawing.Point(6, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "MMC:";
            // 
            // mmcdevice
            // 
            this.mmcdevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.mmcdevice.Enabled = false;
            this.mmcdevice.FormattingEnabled = true;
            this.mmcdevice.Location = new System.Drawing.Point(47, 61);
            this.mmcdevice.Name = "mmcdevice";
            this.mmcdevice.Size = new System.Drawing.Size(188, 21);
            this.mmcdevice.TabIndex = 3;
            // 
            // correctSpareBox
            // 
            this.correctSpareBox.AutoSize = true;
            this.correctSpareBox.Location = new System.Drawing.Point(6, 42);
            this.correctSpareBox.Name = "correctSpareBox";
            this.correctSpareBox.Size = new System.Drawing.Size(90, 17);
            this.correctSpareBox.TabIndex = 1;
            this.correctSpareBox.Text = "Correct Spare";
            this.correctSpareBox.UseVisualStyleBackColor = true;
            // 
            // addSpareBox
            // 
            this.addSpareBox.AutoSize = true;
            this.addSpareBox.Location = new System.Drawing.Point(6, 19);
            this.addSpareBox.Name = "addSpareBox";
            this.addSpareBox.Size = new System.Drawing.Size(75, 17);
            this.addSpareBox.TabIndex = 0;
            this.addSpareBox.Text = "Add Spare";
            this.addSpareBox.UseVisualStyleBackColor = true;
            // 
            // writebtn
            // 
            this.writebtn.Location = new System.Drawing.Point(147, 106);
            this.writebtn.Name = "writebtn";
            this.writebtn.Size = new System.Drawing.Size(129, 23);
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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 319);
            this.Controls.Add(this.optionsbox);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.erasebtn);
            this.Controls.Add(this.writebtn);
            this.Controls.Add(this.readbtn);
            this.Controls.Add(this.devices);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1Load);
            this.devices.ResumeLayout(false);
            this.devices.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.optionsbox.ResumeLayout(false);
            this.optionsbox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox devices;
        private System.Windows.Forms.RadioButton mmc;
        private System.Windows.Forms.RadioButton ftdi;
        private System.Windows.Forms.RadioButton arm;
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox mmcdevice;
        private System.Windows.Forms.RadioButton rawbox;
    }
}

