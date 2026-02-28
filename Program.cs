using System;
using System.Windows.Forms;

namespace MonitorAudioSwitcher
{
    /// <summary>
    /// Entry point class for the MonitorAudioSwitcher application.
    /// Handles the initialization of the application environment and UI thread requirements.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <remarks>
        /// [STAThread] attribute is mandatory for Windows Forms applications 
        /// to set the COM threading model to Single-Threaded Apartment. 
        /// This is required for various UI features and shell interoperability.
        /// </remarks>
        [STAThread]
        static void Main()
        {
            // Ensures the application UI scales correctly according to the system's DPI settings.
            // Critical for multi-monitor setups where screens might have different scaling factors (e.g., 100% vs 150%).
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // Enables visual styles (themes) for the application, ensuring modern Windows control rendering.
            Application.EnableVisualStyles();

            // Sets GDI+ as the default text rendering engine (standard for modern .NET WinForms).
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Starts the standard application message loop on the current thread and opens the main dashboard.
            Application.Run(new Form1()); 
        }
    }
}