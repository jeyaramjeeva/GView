const path = require("path");
const { app, BrowserWindow } = require("electron");
const LOGO_PATH = "C:/Users/Jjeyara/Downloads/Logo.png";

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
      contextIsolation: true,
      sandbox: true,
      devTools: true,
    },
  });

  mainWindow.loadFile(path.join(__dirname, "PlyViewer.html"));
}

app.whenReady().then(() => {
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit();
});

