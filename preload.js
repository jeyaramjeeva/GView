const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("gviewElectron", {
  pickTilesetFolder: () => ipcRenderer.invoke("gview:pick-tileset-folder"),
  startTileServer: (folderPath) => ipcRenderer.invoke("gview:start-tile-server", folderPath),
  stopTileServer: () => ipcRenderer.invoke("gview:stop-tile-server"),
});
