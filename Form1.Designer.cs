namespace MonitorAudioSwitcher
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button startButton;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.startButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(600, 360);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // startButton
            // 
            this.startButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.startButton.Location = new System.Drawing.Point(0, 360);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(600, 40);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Avvia Auto Audio";
            this.startButton.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.startButton);
            this.Name = "Form1";
            this.Text = "Monitor → Audio Mapper";
            this.ResumeLayout(false);
        }
    }
}