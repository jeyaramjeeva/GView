using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GView
{
    public class MainForm : Form
    {
        private WebView2 _webView;
        private readonly string _baseDir;

        public MainForm()
        {
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;

            Text = "GView - Gaussian Splatting & 3D Tiles Viewer";
            Size = new Size(1600, 980);
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(11, 17, 29);

            TryLoadIcon();

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.FromArgb(11, 17, 29)
            };
            Controls.Add(_webView);

            FormClosing += MainForm_FormClosing;
        }

        private void TryLoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(_baseDir, "build", "icon.ico");
                if (!File.Exists(iconPath))
                    iconPath = Path.Combine(_baseDir, "assets", "icon.ico");

                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch { }
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await InitializeWebViewAsync();
        }

        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            try
            {
                string profileDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GView",
                    "WebView2Profile");

                var env = await CoreWebView2Environment.CreateAsync(null, profileDir);
                await _webView.EnsureCoreWebView2Async(env);

                var settings = _webView.CoreWebView2.Settings;
                settings.AreDevToolsEnabled = true;
                settings.IsStatusBarEnabled = false;
                settings.AreDefaultContextMenusEnabled = true;

                // Sync window title with webpage title
                _webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(_webView.CoreWebView2.DocumentTitle))
                    {
                        Text = _webView.CoreWebView2.DocumentTitle;
                    }
                };

                // Notify on navigation errors
                _webView.NavigationCompleted += (s, e) =>
                {
                    if (!e.IsSuccess)
                    {
                        MessageBox.Show(
                            "Failed to load application interface: " + e.WebErrorStatus,
                            "GView Load Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                };

                // Map local repository folder to secure HTTPS origin
                _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "gview.local",
                    _baseDir,
                    CoreWebView2HostResourceAccessKind.Allow);

                // Add COM bridge host object
                _webView.CoreWebView2.AddHostObjectToScript("gviewBridge", new GViewBridge(this, _baseDir));

                // Inject window.gviewElectron bridge so PlyViewer.html functions seamlessly
                string bridgeShim = @"
(function() {
    var bridge = window.chrome.webview.hostObjects.gviewBridge;
    window.gviewElectron = {
        pickTilesetFolder: async function() {
            var res = await bridge.PickTilesetFolder();
            return res ? res : null;
        },
        pickTilesetFile: async function() {
            var res = await bridge.PickTilesetFile();
            return res ? res : null;
        },
        startTileServer: async function(folderOrFile) {
            return await bridge.StartTileServer(folderOrFile);
        },
        stopTileServer: async function() {
            return await bridge.StopTileServer();
        },
        saveFileDialog: async function(options, data) {
            var filter = 'GView Project (*.pvproj)|*.pvproj|All Files (*.*)|*.*';
            var defName = 'model.pvproj';
            if (options) {
                if (options.defaultPath) defName = options.defaultPath;
                if (options.filters && options.filters.length > 0) {
                    var f0 = options.filters[0];
                    filter = f0.name + '|' + f0.extensions.map(function(e) { return '*.' + e; }).join(';') + '|All Files (*.*)|*.*';
                }
            }
            var text = typeof data === 'string' ? data : JSON.stringify(data);
            var res = await bridge.SaveFileDialog(filter, defName, text);
            return res ? res : null;
        },
        openFileDialog: async function(options) {
            var filter = 'All Supported Files|*.ply;*.pvproj;*.json;*.tif;*.tiff|All Files (*.*)|*.*';
            var multi = options && options.multiple;
            var res = await bridge.OpenFileDialog(filter, !!multi);
            return res ? JSON.parse(res) : null;
        },
        closeApp: function() {
            bridge.CloseApp();
        },
        getAppVersion: async function() {
            return '1.3.0';
        },
        getPathForFile: function(file) {
            return file && file.path ? file.path : '';
        }
    };
})();
";
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeShim);

                // Navigate to PlyViewer.html on virtual host
                _webView.CoreWebView2.Navigate("https://gview.local/PlyViewer.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error initializing GView WebView2 environment:\n\n" + ex.Message,
                    "GView Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            TileServer.Instance.Stop();
        }
    }
}
