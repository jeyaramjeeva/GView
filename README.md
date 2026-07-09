# GView

Desktop viewer for PLY / Gaussian Splat models and ArcGIS 3D Tiles Gaussian Splat layers.

## Features

- PLY and Gaussian Splat viewing (Babylon.js)
- 3D Tiles Gaussian Splat (ArcGIS Maps SDK)
- Satellite basemap with model alignment
- Distance and angle measurement tools
- Multi-model projects with camera, transform, and scene controls

## Requirements

- Node.js 18+
- Windows (for the packaged installer)

## Development

```bash
npm install
npm start
```

## Build

```bash
npm run build
```

The Windows installer is written to `dist/` (or a custom `directories.output` folder).

## 3D Tiles

In the desktop app, use **Browse Folder** to pick a local tileset folder. GView starts a built-in CORS-enabled HTTP server and loads `tileset.json` automatically.

For browser-only testing, serve tiles with CORS enabled (for example `npx http-server --cors -g -c-1`).

## License

MIT
