using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AudioSwitcher.AudioApi.CoreAudio;
using System.Management;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Drawing;
using System.Diagnostics;

namespace MonitorAudioSwitcher
{
    /// <summary>
    /// Core logic for monitoring window focus and switching audio endpoints.
    /// Uses Win32 P/Invoke for window tracking and CoreAudio for device routing.
    /// </summary>
    public partial class Form1 : Form
    {
        // Maps a unique monitor string (Name + Bounds) to an Audio Device name
        private Dictionary<string, string> monitorToDevice = new Dictionary<string, string>();
        private List<string> audioDevices = new List<string>();
        private Dictionary<string, ComboBox> combos = new Dictionary<string, ComboBox>();

        private readonly string configPath;
        // Hardcoded path for the development environment icon
        private readonly string iconPath = @"C:\Users\GiUs_\MonitorAudioSwitcher\MonitorAudioSwitcher\MonitorAudioSwitcher.ico";
        private NotifyIcon trayIcon = null!;

        public Form1()
        {
            InitializeComponent();

            // Initialize configuration path in AppData/Roaming to ensure write permissions
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorAudioSwitcher");
            configPath = Path.Combine(folder, "config.json");
            Directory.CreateDirectory(folder);

            SetupTray();
            
            // Sync UI checkbox with Windows Registry "Run" key status
            chkAutoStart.Checked = IsAutoStartEnabled();
            chkAutoStart.CheckedChanged += (s, e) => SetAutoStart(chkAutoStart.Checked);

            // Populate hardware-specific data
            LoadMonitors();
            LoadAudioDevices();
            LoadConfig(); 
            BuildDropdowns();

            startButton.Click += StartButton_Click;
        }

        /// <summary>
        /// Configures the System Tray icon and context menu for background operation.
        /// </summary>
        private void SetupTray()
        {
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Open", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("Exit", null, (s, e) => { Application.Exit(); });

            trayIcon = new NotifyIcon
            {
                Text = "Monitor Audio Switcher",
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            if (File.Exists(iconPath))
                trayIcon.Icon = new Icon(iconPath);
            else
                trayIcon.Icon = SystemIcons.Application; // Fallback to system icon if path is invalid
        }

        /// <summary>
        /// Scans all active screens and creates a unique key using DeviceName and Resolution/Coordinates.
        /// </summary>
        private void LoadMonitors()
        {
            monitorToDevice.Clear();
            foreach (var screen in Screen.AllScreens)
            {
                // Key format: "MonitorName: Left,Top -> Right,Bottom" to uniquely identify displays in multi-head setups
                string key = $"{GetMonitorName(screen)}: {screen.Bounds.Left},{screen.Bounds.Top} -> {screen.Bounds.Right},{screen.Bounds.Bottom}";
                monitorToDevice[key] = "";
            }
        }

        private void LoadAudioDevices()
        {
            var controller = new CoreAudioController();
            audioDevices = controller.GetPlaybackDevices().Select(d => d.Name).ToList();
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(configPath));
                    if (saved != null)
                    {
                        foreach (var kv in saved)
                            if (monitorToDevice.ContainsKey(kv.Key)) monitorToDevice[kv.Key] = kv.Value;
                    }
                }
                catch { /* Ignore corrupted config files */ }
            }
        }

        /// <summary>
        /// Dynamically populates the FlowLayoutPanel with labels and dropdowns for each detected monitor.
        /// </summary>
        private void BuildDropdowns()
        {
            flowLayoutPanel1.Controls.Clear();
            foreach (var monitor in monitorToDevice.Keys)
            {
                var label = new Label { Text = monitor, Width = 300, Margin = new Padding(5) };
                var combo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(5) };
                combo.Items.AddRange(audioDevices.ToArray());

                // Restore previous mapping if available
                if (monitorToDevice.TryGetValue(monitor, out string savedDevice) && combo.Items.Contains(savedDevice))
                    combo.SelectedItem = savedDevice;
                else if (combo.Items.Count > 0) 
                    combo.SelectedIndex = 0;

                combos[monitor] = combo;
                flowLayoutPanel1.Controls.Add(label);
                flowLayoutPanel1.Controls.Add(combo);
            }
        }

        private void StartButton_Click(object? sender, EventArgs e)
        {
            // Update mapping dictionary from UI selections
            foreach (var kv in combos)
            {
                monitorToDevice[kv.Key] = kv.Value.SelectedItem?.ToString() ?? "";
            }

            File.WriteAllText(configPath, JsonSerializer.Serialize(monitorToDevice));

            this.Hide(); // Minimize to Tray

            // Spawn the observer thread. Using a Background thread ensures it closes when the main app exits.
            var thread = new Thread(() => MonitorAudioLoop(new Dictionary<string, string>(monitorToDevice)));
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Background loop that monitors the Active Window and switches audio if it moves to a different monitor.
        /// </summary>
        /// <param name="mapping">Thread-safe copy of the monitor-to-audio device mapping.</param>
        private void MonitorAudioLoop(Dictionary<string, string> mapping)
        {
            IntPtr lastMonitor = IntPtr.Zero;
            string lastAudio = "";
            var controller = new CoreAudioController();

            while (true)
            {
                // Get the handle of the window currently receiving user input
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    // Identify which physical monitor the foreground window is predominantly on
                    // 2 = MONITOR_DEFAULTTONEAREST: Ensures a result even if the window is between screens
                    IntPtr monitor = MonitorFromWindow(hwnd, 2); 

                    if (monitor != lastMonitor)
                    {
                        var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                        if (GetMonitorInfo(monitor, ref mi))
                        {
                            // Reconstruct the key to match the saved dictionary
                            string monitorKey = $"{GetMonitorNameFromRect(mi.rcMonitor)}: {mi.rcMonitor.Left},{mi.rcMonitor.Top} -> {mi.rcMonitor.Right},{mi.rcMonitor.Bottom}";

                            if (mapping.TryGetValue(monitorKey, out string targetDevice))
                            {
                                // Only trigger a switch if the target device is different from the current one to prevent overhead
                                if (!string.IsNullOrEmpty(targetDevice) && targetDevice != lastAudio)
                                {
                                    var dev = controller.GetPlaybackDevices().FirstOrDefault(d => d.Name == targetDevice);
                                    if (dev != null)
                                    {
                                        try
                                        {
                                            dev.SetAsDefault();
                                            lastAudio = targetDevice;
                                        }
                                        catch { /* Handle potential device disconnection during switch */ }
                                    }
                                }
                            }
                            lastMonitor = monitor;
                        }
                    }
                }
                // Throttling the loop to ~2Hz to balance responsiveness and CPU usage (battery friendly)
                Thread.Sleep(500);
            }
        }

        // --- Autostart Helpers (Registry Interop) ---
        private bool IsAutoStartEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("MonitorAudioSwitcher") != null;
        }

        private void SetAutoStart(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable) key?.SetValue("MonitorAudioSwitcher", $"\"{Application.ExecutablePath}\"");
            else key?.DeleteValue("MonitorAudioSwitcher", false);
        }

        // --- Monitor Metadata Management ---
        
        /// <summary>
        /// Attempts to retrieve the User-Friendly Monitor Name (EDID UserFriendlyName) via WMI.
        /// Falls back to DeviceName (e.g., \\.\DISPLAY1) if WMI fails.
        /// </summary>
        private string GetMonitorName(Screen screen)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorID");
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string instance = queryObj["InstanceName"]?.ToString() ?? "";
                    // Cross-reference WMI instance with WinForms Screen DeviceName
                    if (instance.Contains(screen.DeviceName.Replace("\\", "\\\\")))
                    {
                        var nameChars = (ushort[])queryObj["UserFriendlyName"];
                        return string.Concat(nameChars.Where(c => c != 0).Select(c => (char)c));
                    }
                }
            }
            catch { }
            return screen.DeviceName;
        }

        /// <summary>
        /// Matches a Win32 RECT (monitor coordinates) back to a Friendly Name.
        /// Used for identifying the monitor during the background loop.
        /// </summary>
        private string GetMonitorNameFromRect(RECT rect)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Left == rect.Left && screen.Bounds.Top == rect.Top)
                    return GetMonitorName(screen);
            }
            return "Unknown Monitor";
        }

        // --- Win32 API Definitions (User32.dll) ---
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct MONITORINFO { public uint cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
        
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    }
}