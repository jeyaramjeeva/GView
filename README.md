# GView

Desktop viewer for PLY / Gaussian Splat models and ArcGIS 3D Tiles Gaussian Splat layers.

## Features

- PLY and Gaussian Splat viewing (Babylon.js)
- 3D Tiles Gaussian Splat (ArcGIS Maps SDK)
- Satellite basemap with model alignment
- Distance and angle measurement tools
- Multi-model projects with camera, transform, and scene controls

## Requirements

- **Windows 10/11** (for the desktop app)
- **Node.js 18+** (only needed to build or run from source)
- **Git** (to clone the repository)

---

## How to Install (Step by Step)

### Option A — Install the desktop app (recommended)

Use this if you want a normal Windows app with a Start Menu shortcut.

1. **Install Node.js** (if you do not already have it)
   - Download from [https://nodejs.org](https://nodejs.org) (LTS version)
   - Run the installer and keep the default options
   - Open **PowerShell** or **Command Prompt** and verify:
     ```bash
     node -v
     npm -v
     ```

2. **Clone this repository**
   ```bash
   git clone https://github.com/jeyaramjeeva/GView.git
   cd GView
   ```

3. **Install dependencies**
   ```bash
   npm install
   ```
   Wait until it finishes (this downloads Electron and build tools).

4. **Build the Windows installer**
   ```bash
   npm run build
   ```
   - Close any running **GView** or **Electron** windows first, or the build may fail with a file-lock error.
   - When it completes, the installer is created in the `dist` folder.

5. **Run the installer**
   - Open the `dist` folder
   - Double-click **`GView Setup 1.3.0.exe`**
   - Choose an install location when prompted
   - Finish the setup wizard

6. **Launch GView**
   - Open **GView** from the Start Menu, or run it from your chosen install folder.

---

### Option B — Run from source (no installer)

Use this for quick testing or development.

1. **Install Node.js 18+** (see Option A, step 1)

2. **Clone and enter the project**
   ```bash
   git clone https://github.com/jeyaramjeeva/GView.git
   cd GView
   ```

3. **Install dependencies**
   ```bash
   npm install
   ```

4. **Start the app**
   ```bash
   npm start
   ```

GView opens in an Electron window. No installer is created.

---

## First Steps After Install

### Open a PLY / Gaussian Splat model

1. Launch **GView**
2. Go to the **Models** tab
3. Click **Load PLY** (or drag and drop a `.ply` file)
4. Use the mouse to orbit, pan, and zoom the scene

### Open a 3D Tiles Gaussian Splat tileset

1. Go to the **Scene** tab
2. Under **3D Tiles**, click **Browse Folder**
3. Select the folder that contains `tileset.json`
4. GView starts a local CORS-enabled server and loads the tileset automatically
5. Use **Overview**, **Close-up**, or **Zoom** to frame the data

### Optional: satellite basemap (PLY mode)

1. Load a PLY model first
2. In the **Scene** tab, enable **Satellite Basemap**
3. Click **Align to Model** to line up the map with your model

### Measure distances and angles

1. Open the **Measure** tab
2. Choose **Distance** or **Angle**
3. Click points in the 3D view to place measurements

---

## Development

```bash
npm install
npm start
```

## Build installer only

```bash
npm run build
```

Output goes to the `dist` folder (or a custom `directories.output` path if configured).

## 3D Tiles notes

In the desktop app, **Browse Folder** is the easiest way to load local tiles. GView serves `tileset.json` over `http://127.0.0.1` with CORS enabled.

For browser-only testing (without Electron), serve tiles with CORS enabled, for example:

```bash
npx http-server --cors -g -c-1
```

## License

MIT
