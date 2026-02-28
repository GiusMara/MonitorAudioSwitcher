namespace MonitorAudioSwitcher
{
    /// <summary>
    /// Partial class responsible for the UI definition and component initialization.
    /// This file is typically managed by the visual designer but contains the structural 
    /// foundation for the monitor-to-audio mapping interface.
    /// </summary>
    partial class Form1
    {
        /// <summary>
        /// Required designer variable for managing background components and resources.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // UI Controls definitions
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1; // Container for dynamic monitor/audio mapping cards
        private System.Windows.Forms.Button startButton;              // Main toggle for the global Windows hook
        private System.Windows.Forms.CheckBox chkAutoStart;           // Persistence toggle for Windows startup
        private System.Windows.Forms.Button btnOpenConfig;           // Quick access to the local configuration storage

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">
        /// true if managed resources should be disposed; otherwise, false.
        /// Critical for ensuring that UI handles and background hooks are properly released.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// Handles the instantiation, positioning, and styling of the dashboard elements.
        /// </summary>
        private void InitializeComponent()
        {
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.startButton = new System.Windows.Forms.Button();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.btnOpenConfig = new System.Windows.Forms.Button();
            this.SuspendLayout();
            
            // 
            // flowLayoutPanel1
            // 
            // Acts as the dynamic host for monitor-specific settings. 
            // AutoScroll is enabled to support setups with a high number of displays/audio endpoints.
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(10);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(600, 310);
            this.flowLayoutPanel1.TabIndex = 0;
            
            // 
            // chkAutoStart
            // 
            // Logic for this control typically involves interacting with the Windows Registry (HKCU\Software\Microsoft\Windows\CurrentVersion\Run).
            this.chkAutoStart.AutoSize = true;
            this.chkAutoStart.Location = new System.Drawing.Point(15, 325);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(120, 17);
            this.chkAutoStart.TabIndex = 2;
            this.chkAutoStart.Text = "Start with Windows";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            
            // 
            // btnOpenConfig
            // 
            // Provides direct access to the AppData folder for manual configuration or log inspection.
            this.btnOpenConfig.Location = new System.Drawing.Point(450, 320);
            this.btnOpenConfig.Name = "btnOpenConfig";
            this.btnOpenConfig.Size = new System.Drawing.Size(130, 25);
            this.btnOpenConfig.TabIndex = 3;
            this.btnOpenConfig.Text = "Open Config Folder";
            this.btnOpenConfig.UseVisualStyleBackColor = true;
            
            // 
            // startButton
            // 
            // Main trigger for initializing the Win32 Event Hooks (EVENT_OBJECT_LOCATIONCHANGE).
            this.startButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.startButton.Location = new System.Drawing.Point(0, 355);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(600, 45);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "START AUTO AUDIO";
            this.startButton.UseVisualStyleBackColor = true;
            
            // 
            // Form1
            // 
            // The main dashboard is set to FixedSingle to prevent layout distortion during resizing,
            // ensuring a consistent user experience on different display resolutions.
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.chkAutoStart);
            this.Controls.Add(this.btnOpenConfig);
            this.Controls.Add(this.startButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Monitor Audio Switcher";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}