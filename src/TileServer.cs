using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GView
{
    public class TileServer
    {
        private static readonly Lazy<TileServer> _instance = new Lazy<TileServer>(() => new TileServer());
        public static TileServer Instance => _instance.Value;

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private string _rootDirectory;
        private int _port;

        public int Port => _port;
        public string RootDirectory => _rootDirectory;
        public bool IsRunning => _listener != null && _listener.IsListening;

        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".html", "text/html; charset=utf-8" },
            { ".json", "application/json; charset=utf-8" },
            { ".glb", "model/gltf-binary" },
            { ".gltf", "model/gltf+json" },
            { ".b3dm", "application/octet-stream" },
            { ".pnts", "application/octet-stream" },
            { ".i3dm", "application/octet-stream" },
            { ".cmpt", "application/octet-stream" },
            { ".bin", "application/octet-stream" },
            { ".ply", "application/octet-stream" },
            { ".splat", "application/octet-stream" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".webp", "image/webp" },
            { ".ktx2", "image/ktx2" },
            { ".wasm", "application/wasm" },
            { ".txt", "text/plain; charset=utf-8" }
        };

        private TileServer() { }

        public string Start(string rootDir)
        {
            Stop();

            string fullRoot = Path.GetFullPath(rootDir);
            if (!Directory.Exists(fullRoot))
            {
                throw new DirectoryNotFoundException("Tile directory does not exist: " + fullRoot);
            }

            _rootDirectory = fullRoot;
            _port = GetAvailablePort();

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _listener.Start();

            _cts = new CancellationTokenSource();
            Task.Run(() => ListenLoopAsync(_listener, _cts.Token));

            return $"http://127.0.0.1:{_port}/tileset.json";
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                if (_listener != null)
                {
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }
                    _listener.Close();
                    _listener = null;
                }
            }
            catch { }

            _rootDirectory = null;
            _port = 0;
        }

        private async Task ListenLoopAsync(HttpListener listener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    if (ct.IsCancellationRequested) break;
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            try
            {
                res.AddHeader("Access-Control-Allow-Origin", "*");
                res.AddHeader("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
                res.AddHeader("Access-Control-Allow-Headers", "*");
                res.AddHeader("Access-Control-Expose-Headers", "Content-Range, Content-Length, Accept-Ranges");
                res.AddHeader("Cache-Control", "no-store");

                if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    res.StatusCode = 204;
                    res.Close();
                    return;
                }

                if (!req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                    !req.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    res.StatusCode = 405;
                    res.Close();
                    return;
                }

                string rawPath = req.Url.AbsolutePath;
                string relPath = WebUtility.UrlDecode(rawPath);
                if (string.IsNullOrEmpty(relPath) || relPath == "/")
                {
                    relPath = "tileset.json";
                }
                else
                {
                    relPath = relPath.TrimStart('/', '\\');
                }

                string filePath = Path.GetFullPath(Path.Combine(_rootDirectory, relPath));
                if (!filePath.StartsWith(_rootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    res.StatusCode = 403;
                    res.Close();
                    return;
                }

                if (!File.Exists(filePath))
                {
                    res.StatusCode = 404;
                    res.Close();
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                long totalLength = fileInfo.Length;
                string ext = Path.GetExtension(filePath);
                res.ContentType = MimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
                res.AddHeader("Accept-Ranges", "bytes");

                string rangeHeader = req.Headers["Range"];
                if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
                {
                    string rangeSpec = rangeHeader.Substring(6);
                    string[] parts = rangeSpec.Split('-');
                    long start = 0;
                    long end = totalLength - 1;

                    if (!string.IsNullOrEmpty(parts[0]))
                    {
                        long.TryParse(parts[0], out start);
                    }
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        long.TryParse(parts[1], out end);
                    }

                    if (start > end || start >= totalLength)
                    {
                        res.StatusCode = 416;
                        res.AddHeader("Content-Range", $"bytes */{totalLength}");
                        res.Close();
                        return;
                    }

                    long sendLength = end - start + 1;
                    res.StatusCode = 206;
                    res.AddHeader("Content-Range", $"bytes {start}-{end}/{totalLength}");
                    res.ContentLength64 = sendLength;

                    if (!req.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.Seek(start, SeekOrigin.Begin);
                            CopyStream(fs, res.OutputStream, sendLength);
                        }
                    }
                }
                else
                {
                    res.StatusCode = 200;
                    res.ContentLength64 = totalLength;

                    if (!req.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.CopyTo(res.OutputStream);
                        }
                    }
                }

                res.Close();
            }
            catch (Exception)
            {
                try { res.Abort(); } catch { }
            }
        }

        private static void CopyStream(Stream input, Stream output, long bytesToCopy)
        {
            byte[] buffer = new byte[65536];
            long remaining = bytesToCopy;
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = input.Read(buffer, 0, toRead);
                if (read <= 0) break;
                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
