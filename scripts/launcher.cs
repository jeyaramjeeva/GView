using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace GViewLauncher
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);

            string formattedArgs = args != null && args.Length > 0 ? " " + string.Join(" ", args) : "";

            // 1. Try launching the compiled binary in dist\win-unpacked\GView.exe
            string unpackedExe = Path.Combine(baseDir, "dist", "win-unpacked", "GView.exe");
            if (File.Exists(unpackedExe))
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = unpackedExe,
                        Arguments = formattedArgs.Trim(),
                        WorkingDirectory = Path.GetDirectoryName(unpackedExe),
                        UseShellExecute = false
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to launch unpacked GView: " + ex.Message);
                }
            }

            // 2. Try launching via local node_modules electron runtime
            string localElectron = Path.Combine(baseDir, "node_modules", "electron", "dist", "electron.exe");
            if (File.Exists(localElectron))
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = localElectron,
                        Arguments = ("\"" + baseDir.TrimEnd('\\', '/') + "\"" + formattedArgs).Trim(),
                        WorkingDirectory = baseDir,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to launch local electron: " + ex.Message);
                }
            }

            // 3. Fallback to npx electron
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npx electron ." + formattedArgs,
                    WorkingDirectory = baseDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to launch GView:\n\n" + ex.Message + "\n\nPlease ensure dependencies are installed via 'npm install'.",
                    "GView Launcher Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
