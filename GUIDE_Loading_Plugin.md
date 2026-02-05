# How to Load the Owl Plugin in Grasshopper (Rhino 8)

Since this plugin is built using **.NET 7**, there are specific steps required to load it in Rhino 8 on Windows.

## 1. Ensure Rhino 8 is running in .NET Core Mode

By default, Rhino 8 on Windows runs in .NET Framework 4.8 mode. Provide that this plugin is .NET 7, you **must** switch Rhino to .NET Core mode.

1.  Open **Rhino 8**.
2.  Type the command `SetDotNetRuntime`.
3.  Select option **Runtime** and choose **NetCore**.
4.  **Restart Rhino 8** for the changes to take effect.
    *   *Note: You can verify the runtime by looking at the splash screen or typing `SystemInfo` inside Rhino.*

## 2. Generate the Plugin (.gha)

If you haven't already built the project:

1.  Open the terminal in the project directory.
2.  Run:
    ```bash
    dotnet build src/Owl.Grasshopper/Owl.Grasshopper.csproj
    ```
3.  Verify that `Owl.Grasshopper.gha` is created in:
    `src/Owl.Grasshopper/bin/Debug/net7.0/`

## 3. Load the Plugin

There are two main ways to load the plugin.

### Option A: GrasshopperDeveloperSettings (Recommended for Development)

This method allows you to update the code and rebuild without constantly moving files.

1.  Start Rhino 8 (in NetCore mode) and run the `Grasshopper` command.
2.  Run the command `GrasshopperDeveloperSettings` (typed in the Rhino command line).
3.  Uncheck "Memory Load *.GHA assemblies..." if checked (optional, but sometimes helps with debugging files).
4.  Click the **Add Folder** button.
5.  Navigate to and select your build output directory:
    `c:\Users\db\owl\src\Owl.Grasshopper\bin\Debug\net7.0`
    *(Make sure this folder contains `Owl.Grasshopper.gha`)*
6.  Click **OK**.
7.  Restart Rhino 8 (and Grasshopper) to load the new assembly.

### Option B: Manual Installation

1.  Copy `Owl.Grasshopper.gha` AND `Owl.Core.dll` (and any other dependencies from the bin folder).
2.  Paste them into the standard Grasshopper Libraries folder:
    *   **Windows**: `%APPDATA%\Grasshopper\Libraries`
3.  Restart Rhino 8 and Grasshopper.

## Troubleshooting

*   **Plugin doesn't show up?**
    *   Check `SetDotNetRuntime` is set to NetCore.
    *   Check that `Owl.Grasshopper.gha` exists.
    *   Check Rhino command history for any loading errors.
*   **"Assembly load" errors?**
    *   Ensure all dependency DLLs (like `Owl.Core.dll`) are in the same folder as the `.gha`.
