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

            // 1. Try launching the compiled standalone binary in dist\win-unpacked\GView.exe
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
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return;
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
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to launch local electron: " + ex.Message);
                }
            }

            // 3. First-time setup: Dependencies not installed yet on this machine.
            string nodeExe = FindNode(baseDir);
            if (string.IsNullOrEmpty(nodeExe))
            {
                MessageBox.Show(
                    "Node.js runtime was not detected on this system.\n\n" +
                    "To launch GView on this machine, please install Node.js from https://nodejs.org.",
                    "GView - Node.js Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Launch visible setup console to install dependencies and run GView
            try
            {
                string cmdScript = 
                    "title GView First-Time Setup && " +
                    "echo ====================================================================== && " +
                    "echo                GView - First-Time Launch Setup && " +
                    "echo ====================================================================== && " +
                    "echo [INFO] Preparing runtime components from npm... Please wait. && " +
                    "call npm install --no-audit --no-fund && " +
                    "if exist \"node_modules\\electron\\dist\\electron.exe\" ( " +
                    "  start \"\" \"node_modules\\electron\\dist\\electron.exe\" . " + formattedArgs + " " +
                    ") else ( " +
                    "  npx --yes electron . " + formattedArgs + " " +
                    ")";

                var psiSetup = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + cmdScript,
                    WorkingDirectory = baseDir,
                    UseShellExecute = true
                };
                Process.Start(psiSetup);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to start GView setup:\n\n" + ex.Message,
                    "GView Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public static string FindNode(string baseDir)
        {
            string bundledNode = Path.Combine(baseDir, "3D_TILES_Viewer", "Node");
            if (Directory.Exists(bundledNode))
            {
                foreach (var dir in Directory.GetDirectories(bundledNode, "node-v*"))
                {
                    string candidate = Path.Combine(dir, "node.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }

            string[] candidates = new string[]
            {
                @"C:\Program Files\nodejs\node.exe",
                @"C:\Program Files (x86)\nodejs\node.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "nodejs", "node.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "node.exe")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "node",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        string res = p.StandardOutput.ReadLine();
                        p.WaitForExit(1000);
                        if (!string.IsNullOrWhiteSpace(res) && File.Exists(res.Trim()))
                            return res.Trim();
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
