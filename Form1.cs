using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AudioSwitcher.AudioApi.CoreAudio;
using System.Management; // Needed to get monitor names

namespace MonitorAudioSwitcher
{
    public partial class Form1 : Form
    {
        // Maps monitor (name + bounds) to selected audio device
        private Dictionary<string, string> monitorToDevice = new Dictionary<string, string>();

        // List of available audio playback device names
        private List<string> audioDevices = new List<string>();

        // Maps monitor keys to their corresponding ComboBox controls in the GUI
        private Dictionary<string, ComboBox> combos = new Dictionary<string, ComboBox>();

        public Form1()
        {
            InitializeComponent();

            // Load monitors and audio devices at startup
            LoadMonitors();
            LoadAudioDevices();
            BuildDropdowns();

            // Assign Start button event handler
            startButton.Click += StartButton_Click;
        }

        // Load all monitors and initialize mapping
        private void LoadMonitors()
        {
            foreach (var screen in Screen.AllScreens)
            {
                // Format: "MonitorName: Left,Top -> Right,Bottom"
                string key = $"{GetMonitorName(screen)}: {screen.Bounds.Left},{screen.Bounds.Top} -> {screen.Bounds.Right},{screen.Bounds.Bottom}";
                monitorToDevice[key] = "";
            }
        }

        // Load all available playback devices into audioDevices list
        private void LoadAudioDevices()
        {
            var controller = new CoreAudioController();
            var devices = controller.GetPlaybackDevices();
            foreach (var dev in devices)
                audioDevices.Add(dev.Name);
        }

        // Build GUI dropdowns for each monitor
        private void BuildDropdowns()
        {
            foreach (var monitor in monitorToDevice.Keys)
            {
                var label = new Label { Text = monitor, Width = 300, Margin = new Padding(5) };
                var combo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(5) };
                combo.Items.AddRange(audioDevices.ToArray());
                if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                combos[monitor] = combo;

                flowLayoutPanel1.Controls.Add(label);
                flowLayoutPanel1.Controls.Add(combo);
            }
        }

        // Event handler for Start button click
        private void StartButton_Click(object? sender, EventArgs e)
        {
            // Save selected audio device for each monitor
            foreach (var kv in combos)
            {
                var selected = kv.Value.SelectedItem?.ToString();
                if (selected != null)
                    monitorToDevice[kv.Key] = selected;
            }

            MessageBox.Show("Configuration saved. Automatic audio switching will start.");
            Hide();

            // Start background thread to monitor active window and switch audio
            var thread = new Thread(() => MonitorAudioLoop(monitorToDevice));
            thread.IsBackground = true;
            thread.Start();
        }

        // Main monitoring loop
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
                    IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                    if (monitor != lastMonitor)
                    {
                        var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                        if (GetMonitorInfo(monitor, ref mi))
                        {
                            // Build key using monitor name + bounds
                            string monitorKey = $"{GetMonitorNameFromRect(mi.rcMonitor)}: {mi.rcMonitor.Left},{mi.rcMonitor.Top} -> {mi.rcMonitor.Right},{mi.rcMonitor.Bottom}";
                            Console.WriteLine($"[LOG] Active monitor: Handle={monitor.ToInt64()}, Key={monitorKey}");

                            if (mapping.ContainsKey(monitorKey))
                            {
                                string targetDevice = mapping[monitorKey];
                                if (!string.IsNullOrEmpty(targetDevice) && targetDevice != lastAudio)
                                {
                                    var dev = controller.GetPlaybackDevices().FirstOrDefault(d => d.Name == targetDevice);
                                    if (dev != null)
                                    {
                                        try
                                        {
                                            dev.SetAsDefault();
                                            Console.WriteLine($"[LOG] Audio switched to: {targetDevice}");
                                            lastAudio = targetDevice;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[ERROR] Could not switch audio: {ex.Message}");
                                        }
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

        // --- Get monitor friendly name from Screen ---
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
                        string name = string.Concat(nameChars.Where(c => c != 0).Select(c => (char)c));
                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                }
            }
            catch { }
            return screen.DeviceName;
        }

        // --- Get monitor name from RECT (used when only RECT info is available) ---
        private string GetMonitorNameFromRect(RECT rect)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Left == rect.Left &&
                    screen.Bounds.Top == rect.Top &&
                    screen.Bounds.Right == rect.Right &&
                    screen.Bounds.Bottom == rect.Bottom)
                {
                    return GetMonitorName(screen);
                }
            }
            return "Unknown Monitor";
        }

        // --- Monitor interop structs and imports ---
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        const uint MONITOR_DEFAULTTONEAREST = 2;
    }
}