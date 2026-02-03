# MapZoomOnCursor

A Valheim mod that makes the full-screen map (M key) zoom toward the mouse cursor instead of the center of the screen.

## Features

- Scroll wheel zooms toward wherever your cursor is pointing on the large map
- Corner minimap is unaffected

## Installation

1. Install [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) for Valheim
2. Place `MapZoomOnCursor.dll` in your `BepInEx/plugins/` folder
3. Launch Valheim

To disable, remove the DLL from the plugins folder.

## Building from Source

Requires the .NET SDK and a Valheim installation.

1. Clone this repository
2. Verify the `ValheimPath` in `MapZoomOnCursor.csproj` matches your install location
3. Build with `dotnet build` or Visual Studio
4. The DLL is automatically copied to `BepInEx/plugins/` on build
