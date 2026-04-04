using System.Runtime.InteropServices;

namespace BrightnessController;

internal static class Program
{
    private const string MutexName = "BrightnessController_SingleInstance_Mutex";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName,
            out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Brightness Controller is already running.\n" +
                "Look for the sun icon in the system tray (^ arrow).",
                "Already Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (_, e) =>
            HandleUnhandledException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            HandleUnhandledException(e.ExceptionObject as Exception);

        Application.Run(new TrayApplicationContext());

        // Keep mutex alive for entire session
        mutex.ReleaseMutex();
    }

    private static void HandleUnhandledException(Exception? ex)
    {
        if (ex == null) return;
#if DEBUG
        MessageBox.Show(ex.ToString(), "Unhandled Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
        try
        {
            string dir  = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BrightnessController");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"),
                $"[{DateTime.Now:u}] {ex}\n\n");
        }
        catch { }
#endif
    }
}
