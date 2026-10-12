# GView

Desktop viewer for PLY / Gaussian Splat models and ArcGIS 3D Tiles Gaussian Splat layers.

## Features

- **Instant Startup**: Self-contained native Windows executable (`GView.exe`) with zero required runtimes or npm install.
- **AppBridge Integration**: Launch, monitor, and synchronize directly through AppBridge.
- **PLY & Gaussian Splat viewing** powered by Babylon.js with full GPU hardware acceleration.
- **ArcGIS 3D Tiles Gaussian Splat layers** with auto-framing, overview, and satellite basemap.
- **Satellite Basemap alignment** matching model origin to geographic coordinates.
- **Perspective & Orthographic projection modes** with 1:1 view and custom ortho zoom.
- **Ortho Photo & GeoTIFF raster overlay** with opacity blending and fit controls.
- **Interactive 3D distance and 3-point angle measurement tools**.
- **Model hierarchy & multi-selection tools** (box select, brush select, refine, isolate, hide, lock, layer assignments, and double-click rename).
- **Built-in HTTP 3D Tiles Server** with CORS, HTTP 206 Range streaming, and auto-discovery.
- **Native Windows file dialogs** for saving and opening projects (`.pvproj`).
- **Keyboard shortcuts cheat sheet** (press `F1` or click `?` in header).

---

## Quick Start (Zero Setup)

GView is ready to run immediately after cloning:

1. **Clone the repository** (or synchronize via AppBridge):
   ```bash
   git clone https://github.com/jeyaramjeeva/GView.git
   cd GView
   ```

2. **Launch GView**:
   - Double-click **`GView.exe`** (or run `Launch GView.bat`).
   - The application opens instantly with full 3D rendering and tile server capabilities.

---

## First Steps After Launch

### 1. Open a 3D Tileset (e.g. `GS Result\3d_tiles`)

1. Launch **GView**.
2. Click the **3D Tiles (GIS)** button at the top of the controls panel.
3. Choose either:
   - **Browse Folder**: Click **Browse Folder** and select `GS Result` or `GS Result\3d_tiles`.
   - **Select tileset.json**: Click the **Select tileset.json** button and pick `tileset.json`.
   - **Drag & Drop**: Drag `tileset.json` or the `3d_tiles` folder directly into the window.
4. GView automatically starts the local CORS-enabled tile server and streams the Gaussian Splat tileset over the real-world satellite terrain.
5. Use **Overview**, **Close-up**, or **Zoom To Layer** to navigate.

### 2. Open a PLY Splat (e.g. `GS Result\ply\Gaussian_Splats-L0.ply`)

1. Launch **GView**.
2. Click **PLY Splats** at the top of the controls panel.
3. Either drag & drop any `.ply` file into the window, or enter the path / click **Import**.
4. Use the mouse to orbit (Left Drag), pan (Right Drag), and zoom (Wheel).

### 3. Satellite Basemap in PLY mode

1. Load a PLY model.
2. In the **Scene** tab, enable **Satellite Basemap**.
3. Click **Align to Model** to align the satellite map with your model origin.

### 4. Measure distances and angles

1. Open the **Measure** tab.
2. Choose **Distance** or **Angle**.
3. Click points in the 3D view to place measurements.

---

## Alternative: Run via Node.js / Electron (Development)

Developers modifying the web frontend or electron wrappers can also run via npm:

```bash
npm install
npm start
```

To build a standalone Electron installer package:
```bash
npm run build
```

---

## License

MIT
