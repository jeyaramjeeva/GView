using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GView
{
    [ComVisible(true)]
    public class GViewBridge
    {
        private readonly Form _form;
        private readonly string _baseDir;

        public GViewBridge(Form form, string baseDir)
        {
            _form = form;
            _baseDir = baseDir;
        }

        public string PickTilesetFolder()
        {
            return (string)_form.Invoke(new Func<string>(() =>
            {
                using (var fbd = new FolderBrowserDialog
                {
                    Description = "Select 3D Tiles folder (contains tileset.json)",
                    ShowNewFolderButton = false
                })
                {
                    return fbd.ShowDialog(_form) == DialogResult.OK ? fbd.SelectedPath : "";
                }
            }));
        }

        public string PickTilesetFile()
        {
            return (string)_form.Invoke(new Func<string>(() =>
            {
                using (var ofd = new OpenFileDialog
                {
                    Title = "Select 3D Tileset (tileset.json)",
                    Filter = "3D Tileset (*.json)|*.json|All Files (*.*)|*.*",
                    Multiselect = false
                })
                {
                    return ofd.ShowDialog(_form) == DialogResult.OK ? ofd.FileName : "";
                }
            }));
        }

        public string StartTileServer(string folderOrFilePath)
        {
            if (string.IsNullOrWhiteSpace(folderOrFilePath))
                throw new ArgumentException("No folder or file selected.");

            string targetDir = folderOrFilePath;
            if (File.Exists(folderOrFilePath))
            {
                targetDir = Path.GetDirectoryName(folderOrFilePath);
            }

            // Search for tileset.json in subdirectories if not directly present
            if (!File.Exists(Path.Combine(targetDir, "tileset.json")))
            {
                string[] candidates = { "3d_tiles", "tileset", "tiles", "3dtiles" };
                foreach (var sub in candidates)
                {
                    string candidatePath = Path.Combine(targetDir, sub, "tileset.json");
                    if (File.Exists(candidatePath))
                    {
                        targetDir = Path.Combine(targetDir, sub);
                        break;
                    }
                }

                if (!File.Exists(Path.Combine(targetDir, "tileset.json")) && Directory.Exists(targetDir))
                {
                    foreach (var subDir in Directory.GetDirectories(targetDir))
                    {
                        if (File.Exists(Path.Combine(subDir, "tileset.json")))
                        {
                            targetDir = subDir;
                            break;
                        }
                    }
                }
            }

            if (!File.Exists(Path.Combine(targetDir, "tileset.json")))
            {
                throw new FileNotFoundException("tileset.json was not found in: " + targetDir);
            }

            return TileServer.Instance.Start(targetDir);
        }

        public bool StopTileServer()
        {
            TileServer.Instance.Stop();
            return true;
        }

        public string SaveFileDialog(string filter, string defaultName, string data)
        {
            return (string)_form.Invoke(new Func<string>(() =>
            {
                using (var sfd = new SaveFileDialog
                {
                    Title = "Save GView Project",
                    Filter = string.IsNullOrWhiteSpace(filter) ? "GView Project (*.pvproj)|*.pvproj|All Files (*.*)|*.*" : filter,
                    FileName = string.IsNullOrWhiteSpace(defaultName) ? "model.pvproj" : defaultName
                })
                {
                    if (sfd.ShowDialog(_form) == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, data ?? "", Encoding.UTF8);
                        return sfd.FileName;
                    }
                    return "";
                }
            }));
        }

        public string OpenFileDialog(string filter, bool multiple)
        {
            return (string)_form.Invoke(new Func<string>(() =>
            {
                using (var ofd = new OpenFileDialog
                {
                    Title = "Open File",
                    Filter = string.IsNullOrWhiteSpace(filter) ? "All Supported Files|*.ply;*.pvproj;*.json;*.tif;*.tiff|All Files (*.*)|*.*" : filter,
                    Multiselect = multiple
                })
                {
                    if (ofd.ShowDialog(_form) == DialogResult.OK)
                    {
                        var files = ofd.FileNames;
                        var sb = new StringBuilder();
                        sb.Append("[");
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (i > 0) sb.Append(",");
                            sb.Append("\"").Append(files[i].Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\"");
                        }
                        sb.Append("]");
                        return sb.ToString();
                    }
                    return "";
                }
            }));
        }

        public void CloseApp()
        {
            _form.BeginInvoke(new Action(() => _form.Close()));
        }

        public string GetAppVersion()
        {
            return "1.3.0";
        }
    }
}
