const { contextBridge, ipcRenderer, webUtils } = require("electron");

contextBridge.exposeInMainWorld("gviewElectron", {
  pickTilesetFolder: () => ipcRenderer.invoke("gview:pick-tileset-folder"),
  pickTilesetFile: () => ipcRenderer.invoke("gview:pick-tileset-file"),
  startTileServer: (targetPath) => ipcRenderer.invoke("gview:start-tile-server", targetPath),
  stopTileServer: () => ipcRenderer.invoke("gview:stop-tile-server"),
  saveFileDialog: (options, data) => ipcRenderer.invoke("gview:save-file-dialog", options, data),
  openFileDialog: (options) => ipcRenderer.invoke("gview:open-file-dialog", options),
  closeApp: () => ipcRenderer.invoke("gview:close-app"),
  getAppVersion: () => ipcRenderer.invoke("gview:get-version"),
  getPathForFile: (file) => {
    try {
      if (webUtils && typeof webUtils.getPathForFile === "function") {
        return webUtils.getPathForFile(file);
      }
    } catch (_) {}
    return file?.path || "";
  },
});
