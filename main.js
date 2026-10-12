const path = require("path");
const fs = require("fs");
const http = require("http");
const { app, BrowserWindow, dialog, ipcMain } = require("electron");

const ICON_PATH = process.platform === "win32" && fs.existsSync(path.join(__dirname, "assets", "icon.ico"))
  ? path.join(__dirname, "assets", "icon.ico")
  : (fs.existsSync(path.join(__dirname, "assets", "icon.png"))
    ? path.join(__dirname, "assets", "icon.png")
    : (fs.existsSync(path.join(__dirname, "build", "icon.ico"))
      ? path.join(__dirname, "build", "icon.ico")
      : undefined));

let tileServer = null;
let tileServerRoot = "";
let mainWindow = null;

const MIME_TYPES = {
  ".html": "text/html; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".glb": "model/gltf-binary",
  ".gltf": "model/gltf+json",
  ".b3dm": "application/octet-stream",
  ".pnts": "application/octet-stream",
  ".i3dm": "application/octet-stream",
  ".cmpt": "application/octet-stream",
  ".bin": "application/octet-stream",
  ".ply": "application/octet-stream",
  ".splat": "application/octet-stream",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".webp": "image/webp",
  ".ktx2": "image/ktx2",
  ".wasm": "application/wasm",
  ".txt": "text/plain; charset=utf-8",
};

function stopTileServer() {
  if (!tileServer) return;
  try {
    if (typeof tileServer.closeAllConnections === "function") {
      tileServer.closeAllConnections();
    }
    tileServer.close();
  } catch (_) {}
  tileServer = null;
  tileServerRoot = "";
}

function startTileServer(rootDir) {
  return new Promise((resolve, reject) => {
    stopTileServer();
    const root = path.resolve(rootDir);
    if (!fs.existsSync(root)) {
      reject(new Error("Folder does not exist."));
      return;
    }

    const server = http.createServer((req, res) => {
      res.setHeader("Access-Control-Allow-Origin", "*");
      res.setHeader("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
      res.setHeader("Access-Control-Allow-Headers", "*");
      res.setHeader("Access-Control-Expose-Headers", "Content-Range, Content-Length, Accept-Ranges");

      if (req.method === "OPTIONS") {
        res.writeHead(204);
        res.end();
        return;
      }
      if (req.method !== "GET" && req.method !== "HEAD") {
        res.writeHead(405);
        res.end("Method not allowed");
        return;
      }

      let reqPath = "/";
      try {
        reqPath = decodeURIComponent(new URL(req.url, "http://127.0.0.1").pathname);
      } catch (_) {
        res.writeHead(400);
        res.end("Bad request");
        return;
      }

      if (reqPath === "/") reqPath = "/tileset.json";
      const rel = reqPath.replace(/^\/+/, "");
      const filePath = path.resolve(root, rel);
      const rootResolved = path.resolve(root);
      const relCheck = path.relative(rootResolved, filePath);

      if (relCheck.startsWith("..") || path.isAbsolute(relCheck)) {
        res.writeHead(403);
        res.end("Forbidden");
        return;
      }

      fs.stat(filePath, (err, stat) => {
        if (err || !stat.isFile()) {
          res.writeHead(404);
          res.end("Not found");
          return;
        }

        const ext = path.extname(filePath).toLowerCase();
        const type = MIME_TYPES[ext] || "application/octet-stream";
        const total = stat.size;
        const rangeHeader = req.headers.range;

        if (rangeHeader) {
          const parts = rangeHeader.replace(/bytes=/, "").split("-");
          const start = parseInt(parts[0], 10);
          const end = parts[1] ? parseInt(parts[1], 10) : total - 1;

          if (isNaN(start) || isNaN(end) || start > end || start >= total) {
            res.writeHead(416, { "Content-Range": `bytes */${total}` });
            res.end();
            return;
          }

          res.writeHead(206, {
            "Content-Range": `bytes ${start}-${end}/${total}`,
            "Accept-Ranges": "bytes",
            "Content-Length": end - start + 1,
            "Content-Type": type,
            "Cache-Control": "no-store",
          });

          if (req.method === "HEAD") {
            res.end();
            return;
          }

          fs.createReadStream(filePath, { start, end }).pipe(res);
        } else {
          res.writeHead(200, {
            "Accept-Ranges": "bytes",
            "Content-Length": total,
            "Content-Type": type,
            "Cache-Control": "no-store",
          });

          if (req.method === "HEAD") {
            res.end();
            return;
          }

          fs.createReadStream(filePath).pipe(res);
        }
      });
    });

    server.on("error", reject);
    server.listen(0, "127.0.0.1", () => {
      tileServer = server;
      tileServerRoot = root;
      const port = server.address().port;
      resolve(`http://127.0.0.1:${port}/tileset.json`);
    });
  });
}

app.setName("GView");

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1600,
    height: 980,
    minWidth: 1100,
    minHeight: 700,
    backgroundColor: "#0b111d",
    autoHideMenuBar: true,
    icon: ICON_PATH,
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      sandbox: true,
      devTools: true,
    },
  });

  mainWindow.loadFile(path.join(__dirname, "PlyViewer.html"));

  mainWindow.webContents.on("console-message", (_event, level, message, line, sourceId) => {
    const lvl = ["VERBOSE", "INFO", "WARN", "ERROR"][level] || "LOG";
    console.log(`[Renderer ${lvl}] ${message} (${path.basename(sourceId || "")}:${line})`);
  });

  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}

ipcMain.handle("gview:pick-tileset-folder", async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ["openDirectory"],
    title: "Select 3D Tiles folder (contains tileset.json)",
  });
  if (result.canceled || !result.filePaths.length) return null;
  return result.filePaths[0];
});

ipcMain.handle("gview:pick-tileset-file", async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ["openFile"],
    title: "Select 3D Tileset (tileset.json)",
    filters: [
      { name: "3D Tileset (*.json)", extensions: ["json"] },
      { name: "All Files (*.*)", extensions: ["*"] },
    ],
  });
  if (result.canceled || !result.filePaths.length) return null;
  return result.filePaths[0];
});

ipcMain.handle("gview:start-tile-server", async (_event, folderOrFilePath) => {
  if (!folderOrFilePath) throw new Error("No folder or file selected.");
  let targetDir = folderOrFilePath;
  try {
    if (fs.existsSync(folderOrFilePath) && fs.statSync(folderOrFilePath).isFile()) {
      targetDir = path.dirname(folderOrFilePath);
    }
  } catch (_) {}

  // If tileset.json is not immediately in targetDir, check subdirectories
  if (!fs.existsSync(path.join(targetDir, "tileset.json"))) {
    try {
      const candidates = ["3d_tiles", "tileset", "tiles", "3dtiles"];
      for (const sub of candidates) {
        if (fs.existsSync(path.join(targetDir, sub, "tileset.json"))) {
          targetDir = path.join(targetDir, sub);
          break;
        }
      }
      if (!fs.existsSync(path.join(targetDir, "tileset.json"))) {
        const subDirs = fs.readdirSync(targetDir, { withFileTypes: true });
        for (const dir of subDirs) {
          if (dir.isDirectory() && fs.existsSync(path.join(targetDir, dir.name, "tileset.json"))) {
            targetDir = path.join(targetDir, dir.name);
            break;
          }
        }
      }
    } catch (_) {}
  }
  const tilesetPath = path.join(targetDir, "tileset.json");
  if (!fs.existsSync(tilesetPath)) {
    throw new Error("tileset.json was not found in: " + targetDir);
  }
  return startTileServer(targetDir);
});

ipcMain.handle("gview:stop-tile-server", async () => {
  stopTileServer();
  return true;
});

ipcMain.handle("gview:save-file-dialog", async (_event, options = {}, data = "") => {
  const result = await dialog.showSaveDialog(mainWindow, {
    title: options.title || "Save File",
    defaultPath: options.defaultPath || "model.pvproj",
    filters: options.filters || [{ name: "Project", extensions: ["pvproj", "json"] }],
  });
  if (result.canceled || !result.filePath) return null;
  if (typeof data === "string") {
    fs.writeFileSync(result.filePath, data, "utf8");
  } else if (Buffer.isBuffer(data)) {
    fs.writeFileSync(result.filePath, data);
  }
  return result.filePath;
});

ipcMain.handle("gview:open-file-dialog", async (_event, options = {}) => {
  const result = await dialog.showOpenDialog(mainWindow, {
    title: options.title || "Open File",
    properties: options.multiple ? ["openFile", "multiSelections"] : ["openFile"],
    filters: options.filters || [{ name: "All Supported", extensions: ["ply", "pvproj", "json", "tif", "tiff"] }],
  });
  if (result.canceled || !result.filePaths.length) return null;
  return result.filePaths;
});

ipcMain.handle("gview:close-app", async () => {
  stopTileServer();
  app.quit();
});

ipcMain.handle("gview:get-version", async () => {
  return app.getVersion();
});

app.whenReady().then(() => {
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  stopTileServer();
  if (process.platform !== "darwin") app.quit();
});

app.on("before-quit", () => {
  stopTileServer();
});
