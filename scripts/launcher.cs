using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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

            // 3. First-time setup: Dependencies not installed yet on this machine.
            string nodeExe = FindNode(baseDir);
            if (string.IsNullOrEmpty(nodeExe))
            {
                MessageBox.Show(
                    "Node.js runtime was not detected on this system.\n\n" +
                    "To run GView, please install Node.js from https://nodejs.org or download the standalone package.",
                    "GView - Node.js Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Node.js is present. Run setup splash dialog.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var splash = new SetupSplashForm(baseDir, formattedArgs, nodeExe))
            {
                Application.Run(splash);
            }
        }

        public static string FindNode(string baseDir)
        {
            // Check bundled portable node if present
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

    public class SetupSplashForm : Form
    {
        private readonly string _baseDir;
        private readonly string _formattedArgs;
        private readonly string _nodeExe;
        private Label _lblStatus;
        private ProgressBar _prog;

        public SetupSplashForm(string baseDir, string formattedArgs, string nodeExe)
        {
            _baseDir = baseDir;
            _formattedArgs = formattedArgs;
            _nodeExe = nodeExe;

            this.Text = "GView - First-Time Setup";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(420, 140);
            this.BackColor = Color.FromArgb(248, 250, 252);

            var lblTitle = new Label
            {
                Text = "Setting up GView...",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 16),
                AutoSize = true
            };

            _lblStatus = new Label
            {
                Text = "Installing runtime components... This only takes a moment.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 46),
                Size = new Size(380, 24)
            };

            _prog = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Location = new Point(20, 78),
                Size = new Size(380, 24)
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_prog);

            this.Load += (s, e) => ExecuteSetupAsync();
        }

        private void ExecuteSetupAsync()
        {
            var bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                try
                {
                    string nodeDir = Path.GetDirectoryName(_nodeExe);
                    string npmCmd = Path.Combine(nodeDir, "npm.cmd");
                    string cmdFile = File.Exists(npmCmd) ? npmCmd : "npm";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c " + cmdFile + " install --no-audit --no-fund",
                        WorkingDirectory = _baseDir,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    // Add node dir to PATH in environment if bundled node is used
                    string currentPath = psi.EnvironmentVariables["PATH"] ?? "";
                    if (!currentPath.Contains(nodeDir))
                    {
                        psi.EnvironmentVariables["PATH"] = nodeDir + ";" + currentPath;
                    }

                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null) proc.WaitForExit();
                    }
                }
                catch { }
            };

            bw.RunWorkerCompleted += (s, e) =>
            {
                this.Hide();

                string localElectron = Path.Combine(_baseDir, "node_modules", "electron", "dist", "electron.exe");
                if (File.Exists(localElectron))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = localElectron,
                            Arguments = ("\"" + _baseDir.TrimEnd('\\', '/') + "\"" + _formattedArgs).Trim(),
                            WorkingDirectory = _baseDir,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (var p = Process.Start(psi))
                        {
                            if (p != null) p.WaitForExit();
                        }
                        this.Close();
                        return;
                    }
                    catch { }
                }

                // Fallback: npx --yes electron .
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c npx --yes electron ." + _formattedArgs,
                        WorkingDirectory = _baseDir,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using (var p = Process.Start(psi))
                    {
                        if (p != null) p.WaitForExit();
                    }
                }
                catch { }

                this.Close();
            };

            bw.RunWorkerAsync();
        }
    }
}
