//auto generated

namespace ParticleAcceleratorMonitoring
{
    partial class MonitoringService
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
            this.ConsoleTextbox = new System.Windows.Forms.TextBox();
            this.PingButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ConsoleTextbox
            // 
            this.ConsoleTextbox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(42)))), ((int)(((byte)(64)))));
            this.ConsoleTextbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ConsoleTextbox.Dock = System.Windows.Forms.DockStyle.Right;
            this.ConsoleTextbox.ForeColor = System.Drawing.Color.White;
            this.ConsoleTextbox.Location = new System.Drawing.Point(102, 0);
            this.ConsoleTextbox.Multiline = true;
            this.ConsoleTextbox.Name = "ConsoleTextbox";
            this.ConsoleTextbox.ReadOnly = true;
            this.ConsoleTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ConsoleTextbox.Size = new System.Drawing.Size(704, 450);
            this.ConsoleTextbox.TabIndex = 1;
            // 
            // PingButton
            // 
            this.PingButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.PingButton.FlatAppearance.BorderSize = 0;
            this.PingButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PingButton.Font = new System.Drawing.Font("Nirmala UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PingButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(126)))), ((int)(((byte)(249)))));
            this.PingButton.Location = new System.Drawing.Point(0, 0);
            this.PingButton.Name = "PingButton";
            this.PingButton.Size = new System.Drawing.Size(102, 52);
            this.PingButton.TabIndex = 4;
            this.PingButton.Text = "Ping";
            this.PingButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.PingButton.UseVisualStyleBackColor = true;
            this.PingButton.Click += new System.EventHandler(this.PingAll_Click);
            // 
            // MonitoringService
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(30)))), ((int)(((byte)(54)))));
            this.ClientSize = new System.Drawing.Size(806, 450);
            this.Controls.Add(this.PingButton);
            this.Controls.Add(this.ConsoleTextbox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MonitoringService";
            this.Text = "Monitoring Service";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MonitoringService_FormClosing);
            this.Load += new System.EventHandler(this.MonitoringService_Load);
            this.Shown += new System.EventHandler(this.MonitoringService_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox ConsoleTextbox;
        private System.Windows.Forms.Button PingButton;
    }
}