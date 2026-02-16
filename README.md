# Owl - Tribune Generator

**Owl** is a parametric Grasshopper plugin for Rhino 8, designed to automate the generation and validation of tribune (seating) geometry for auditoriums, cinemas, and stadiums. It creates detailed 3D models including steps, aisles, railings, and seating.

## Key Features

- **Tribune Generation**: Parametric control over row counts, widths, and elevation steps.
- **Stair & Aisle Logic**: Automatically generates aisles with correct step dimensions (risers and treads).
- **Railing Generation**: Creates railings based on row configurations and safety requirements.
- **Audience & Seating**: Populates tribunes with seats (e.g., recliners) and audience members.
- **Validation**:
    - **Sightline Analysis**: Calculates C-values and visibility from every seat to a screen or focal point.
    - **Geometry Check**: Validates clearance, step heights, and compliance with safety standards.

## Prerequisites

- **Rhino 8** (Running in .NET Core mode)
- **.NET 7.0 SDK** (For building from source)

## Installation

### Method 1: Build from Source (Recommended)

1.  **Clone the repository**:
    ```bash
    git clone <repository_url>
    cd owl
    ```

2.  **Build the project**:
    ```bash
    dotnet build src/Owl.Grasshopper/Owl.Grasshopper.csproj
    ```

3.  **Load in Grasshopper**:
    - Open Rhino 8.
    - Run `SetDotNetRuntime` and select **NetCore**. Restart Rhino if changed.
    - Run `Grasshopper` -> `GrasshopperDeveloperSettings`.
    - Add the bin folder: `.../src/Owl.Grasshopper/bin/Debug/net7.0`.
    - Restart Rhino/Grasshopper.

### Method 2: Manual Install

1.  Download the latest release.
2.  Copy `Owl.Grasshopper.gha` and `Owl.Core.dll` to your Grasshopper Libraries folder (`%APPDATA%\Grasshopper\Libraries`).
3.  Ensure Rhino 8 is running in **NetCore** mode.

## Usage

Owl components are available under the **Owl** tab in Grasshopper. The workflow typically follows this pattern:

1.  **Setup**: Use components like `TribuneSetup`, `StairSetup`, and `RailingSetup` to define parameters.
2.  **Solve**: Connect setup objects to `TribuneSolver` to generate geometry.
3.  **Validate**: Use `Validator` to check for issues and analyze sightlines.
