using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace GView
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private static string _logFilePath;

        [STAThread]
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);

            InitLogger(baseDir);
            Log("=======================================================");
            Log($"GView v1.3.0 starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Log($"BaseDirectory: {baseDir}");
            Log($"OSVersion: {Environment.OSVersion}");
            Log($"CLR Version: {Environment.Version}");
            Log($"Is64BitProcess: {Environment.Is64BitProcess}");

            // Global exception traps
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                string msg = "UnhandledException: " + e.ExceptionObject;
                Log(msg);
                MessageBox.Show(
                    "GView encountered an unhandled exception:\n\n" + e.ExceptionObject,
                    "GView Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                string msg = "ThreadException: " + e.Exception;
                Log(msg);
                MessageBox.Show(
                    "GView encountered a thread exception:\n\n" + e.Exception,
                    "GView Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            // Custom assembly resolution
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;

            // Pre-load WebView2 native loader DLL
            PreloadNativeLoader(baseDir);

            try
            {
                RunApplication();
            }
            catch (Exception ex)
            {
                Log("Exception in RunApplication: " + ex);
                MessageBox.Show(
                    "GView failed to start:\n\n" + ex.Message + "\n\nSee log: " + _logFilePath,
                    "GView Launch Failure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunApplication()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Log("Checking Edge WebView2 runtime availability...");
            string wvVersion = null;
            try
            {
                wvVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                Log("WebView2 version found: " + wvVersion);
            }
            catch (Exception ex)
            {
                Log("WebView2 detection error: " + ex.Message);
            }

            if (string.IsNullOrEmpty(wvVersion))
            {
                Log("WebView2 runtime missing. Prompting user for automatic installation...");
                var choice = MessageBox.Show(
                    "GView requires the Microsoft Edge WebView2 runtime component to display 3D models.\n\n" +
                    "Would you like GView to automatically download and install it now? (Takes ~10 seconds)",
                    "GView - Component Setup Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    bool installed = InstallWebView2Bootstrapper();
                    if (!installed) return;
                }
                else
                {
                    MessageBox.Show(
                        "GView cannot display 3D models without Microsoft Edge WebView2.\n\n" +
                        "Please download and install it manually from:\n" +
                        "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                        "GView - WebView2 Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
            }

            Log("Launching MainForm...");
            Application.Run(new MainForm());
            Log("Application exited normally.");
        }

        private static bool InstallWebView2Bootstrapper()
        {
            try
            {
                string tempExe = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe");
                Log("Downloading WebView2 Evergreen Bootstrapper to: " + tempExe);

                // Enable TLS 1.2
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                using (var client = new WebClient())
                {
                    client.DownloadFile("https://go.microsoft.com/fwlink/p/?LinkId=2124703", tempExe);
                }

                Log("Starting silent WebView2 installation...");
                var psi = new ProcessStartInfo
                {
                    FileName = tempExe,
                    Arguments = "/silent /install",
                    UseShellExecute = true
                };

                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(120000); // Wait up to 2 minutes
                }

                string newVer = CoreWebView2Environment.GetAvailableBrowserVersionString();
                Log("WebView2 installed successfully. New version: " + newVer);
                return !string.IsNullOrEmpty(newVer);
            }
            catch (Exception ex)
            {
                Log("Failed to install WebView2 automatically: " + ex);
                MessageBox.Show(
                    "Automatic installation of WebView2 failed:\n" + ex.Message + "\n\n" +
                    "Please download and install it manually from:\n" +
                    "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                    "Installation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private static void PreloadNativeLoader(string baseDir)
        {
            string[] candidatePaths = new[]
            {
                Path.Combine(baseDir, "runtimes", "win-x64", "native", "WebView2Loader.dll"),
                Path.Combine(baseDir, "x64", "WebView2Loader.dll"),
                Path.Combine(baseDir, "WebView2Loader.dll")
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    IntPtr hModule = LoadLibrary(path);
                    if (hModule != IntPtr.Zero)
                    {
                        Log("Pre-loaded native WebView2Loader.dll from: " + path);
                        return;
                    }
                }
            }

            Log("Warning: Could not pre-load WebView2Loader.dll from known candidate paths.");
        }

        private static Assembly ResolveAssemblies(object sender, ResolveEventArgs args)
        {
            var requestedName = new AssemblyName(args.Name).Name;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string candidate = Path.Combine(baseDir, requestedName + ".dll");
            if (File.Exists(candidate))
            {
                try
                {
                    Log("Resolved assembly from disk: " + candidate);
                    return Assembly.LoadFrom(candidate);
                }
                catch { }
            }

            return null;
        }

        private static void InitLogger(string baseDir)
        {
            try
            {
                _logFilePath = Path.Combine(baseDir, "gview_debug.log");
                // Test write
                File.AppendAllText(_logFilePath, $"--- Log Session Init: {DateTime.Now} ---\r\n");
            }
            catch
            {
                try
                {
                    string localDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "GView");
                    Directory.CreateDirectory(localDir);
                    _logFilePath = Path.Combine(localDir, "gview_debug.log");
                }
                catch { }
            }
        }

        public static void Log(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(_logFilePath))
                {
                    string entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n";
                    File.AppendAllText(_logFilePath, entry);
                }
            }
            catch { }
        }
    }
}
