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
    public partial class Form1 : Form
    {
        private Dictionary<string, string> monitorToDevice = new Dictionary<string, string>();
        private List<string> audioDevices = new List<string>();
        private Dictionary<string, ComboBox> combos = new Dictionary<string, ComboBox>();

        private readonly string configPath;
        private readonly string iconPath = @"C:\Users\GiUs_\MonitorAudioSwitcher\MonitorAudioSwitcher\MonitorAudioSwitcher.ico";
        private NotifyIcon trayIcon = null!;

        public Form1()
        {
            InitializeComponent();

            // Percorso config in AppData
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorAudioSwitcher");
            configPath = Path.Combine(folder, "config.json");
            Directory.CreateDirectory(folder);

            SetupTray();
            
            // Check autostart esistente
            chkAutoStart.Checked = IsAutoStartEnabled();
            chkAutoStart.CheckedChanged += (s, e) => SetAutoStart(chkAutoStart.Checked);

            LoadMonitors();
            LoadAudioDevices();
            LoadConfig(); // Carica prima di costruire la UI
            BuildDropdowns();

            startButton.Click += StartButton_Click;
        }

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
                trayIcon.Icon = SystemIcons.Application;
        }

        private void LoadMonitors()
        {
            monitorToDevice.Clear();
            foreach (var screen in Screen.AllScreens)
            {
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
                catch { }
            }
        }

        private void BuildDropdowns()
        {
            flowLayoutPanel1.Controls.Clear();
            foreach (var monitor in monitorToDevice.Keys)
            {
                var label = new Label { Text = monitor, Width = 300, Margin = new Padding(5) };
                var combo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(5) };
                combo.Items.AddRange(audioDevices.ToArray());

                // Applica selezione salvata
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
            foreach (var kv in combos)
            {
                monitorToDevice[kv.Key] = kv.Value.SelectedItem?.ToString() ?? "";
            }

            // Salva JSON
            File.WriteAllText(configPath, JsonSerializer.Serialize(monitorToDevice));

            this.Hide();

            var thread = new Thread(() => MonitorAudioLoop(new Dictionary<string, string>(monitorToDevice)));
            thread.IsBackground = true;
            thread.Start();
        }

        private void MonitorAudioLoop(Dictionary<string, string> mapping)
        {
            IntPtr lastMonitor = IntPtr.Zero;
            string lastAudio = "";
            var controller = new CoreAudioController();

            while (true)
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr monitor = MonitorFromWindow(hwnd, 2); // MONITOR_DEFAULTTONEAREST

                    if (monitor != lastMonitor)
                    {
                        var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                        if (GetMonitorInfo(monitor, ref mi))
                        {
                            string monitorKey = $"{GetMonitorNameFromRect(mi.rcMonitor)}: {mi.rcMonitor.Left},{mi.rcMonitor.Top} -> {mi.rcMonitor.Right},{mi.rcMonitor.Bottom}";

                            if (mapping.TryGetValue(monitorKey, out string targetDevice))
                            {
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
                                        catch { }
                                    }
                                }
                            }
                            lastMonitor = monitor;
                        }
                    }
                }
                Thread.Sleep(500);
            }
        }

        // --- Helper per Autostart ---
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

        // --- Gestione Nomi Monitor ---
        private string GetMonitorName(Screen screen)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorID");
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string instance = queryObj["InstanceName"]?.ToString() ?? "";
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

        private string GetMonitorNameFromRect(RECT rect)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Left == rect.Left && screen.Bounds.Top == rect.Top)
                    return GetMonitorName(screen);
            }
            return "Unknown Monitor";
        }

        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct MONITORINFO { public uint cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    }
}