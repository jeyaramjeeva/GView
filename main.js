const path = require("path");
const fs = require("fs");
const http = require("http");
const { app, BrowserWindow, dialog, ipcMain } = require("electron");

const LOGO_PATH = "C:/Users/Jjeyara/Downloads/LogoNew.png";

let tileServer = null;
let tileServerRoot = "";

const MIME_TYPES = {
  ".html": "text/html; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".glb": "model/gltf-binary",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".bat": "text/plain; charset=utf-8",
};

function stopTileServer() {
  if (!tileServer) return;
  tileServer.close();
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
      res.setHeader("Access-Control-Allow-Headers", "Content-Type");
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
      const filePath = path.normalize(path.join(root, rel));
      const rootNorm = path.normalize(root + path.sep);
      if (!filePath.startsWith(rootNorm) && filePath !== path.normalize(root)) {
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
        res.writeHead(200, { "Content-Type": type, "Cache-Control": "no-store" });
        if (req.method === "HEAD") {
          res.end();
          return;
        }
        fs.createReadStream(filePath).pipe(res);
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
  const mainWindow = new BrowserWindow({
    width: 1600,
    height: 980,
    minWidth: 1100,
    minHeight: 700,
    backgroundColor: "#0b111d",
    autoHideMenuBar: true,
    icon: LOGO_PATH,
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      sandbox: true,
      devTools: true,
    },
  });

  mainWindow.loadFile(path.join(__dirname, "PlyViewer.html"));
}

ipcMain.handle("gview:pick-tileset-folder", async () => {
  const result = await dialog.showOpenDialog({
    properties: ["openDirectory"],
    title: "Select 3D Tiles folder (contains tileset.json)",
  });
  if (result.canceled || !result.filePaths.length) return null;
  return result.filePaths[0];
});

ipcMain.handle("gview:start-tile-server", async (_event, folderPath) => {
  if (!folderPath) throw new Error("No folder selected.");
  const tilesetPath = path.join(folderPath, "tileset.json");
  if (!fs.existsSync(tilesetPath)) {
    throw new Error("tileset.json was not found in the selected folder.");
  }
  return startTileServer(folderPath);
});

ipcMain.handle("gview:stop-tile-server", async () => {
  stopTileServer();
  return true;
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
