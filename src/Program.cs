using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace GView
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);

            // Register assembly resolver for embedded or side-by-side DLLs
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Verify WebView2 availability
            try
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (string.IsNullOrEmpty(version))
                {
                    ShowWebView2MissingDialog();
                    return;
                }
            }
            catch (Exception)
            {
                ShowWebView2MissingDialog();
                return;
            }

            Application.Run(new MainForm());
        }

        private static void ShowWebView2MissingDialog()
        {
            MessageBox.Show(
                "Microsoft Edge WebView2 Runtime is required to run GView.\n\n" +
                "Please download and install WebView2 from:\n" +
                "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                "GView - WebView2 Runtime Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static Assembly ResolveAssemblies(object sender, ResolveEventArgs args)
        {
            var requestedName = new AssemblyName(args.Name).Name;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Direct file in baseDir
            string candidate = Path.Combine(baseDir, requestedName + ".dll");
            if (File.Exists(candidate))
            {
                try { return Assembly.LoadFrom(candidate); } catch { }
            }

            // 2. Embedded manifest resource if bundled
            try
            {
                string resourceName = "GView." + requestedName + ".dll";
                var currentAssembly = Assembly.GetExecutingAssembly();
                using (var stream = currentAssembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        return Assembly.Load(bytes);
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
