# Owl - Tribune & Audience Analysis Plugin

**Owl** is a Rhino 8 Grasshopper plugin developed for the parametric design and analysis of auditoriums, cinemas, and grandstands. It focuses on section-based tribune generation, sightline analysis (C-Values), and plan-based chair distribution.

## Features

### 1. Tribune Design (Section)
-   Parametric definition of rows, rise steps, and depth.
-   Renovation logic to quantize vertical geometry to valid riser heights.
-   Visualization of tribune, stairs, and railings in section.

### 2. Sightline Analysis
-   Calculates C-Values (clearance over the head of the spectator in front).
-   Visualizes sightlines from eye points to a screen or focal point.
-   Projector cone analysis to detect blockage.

### 3. Plan Distribution **[NEW]**
-   **Plan Setup**: Define the seating boundary, aisle (voids), and structural tunnels (exclusions).
-   **Distribution**: Automatically generates plan geometry based on the section logic.
    -   **Tribune Lines**: Clipped by boundaries.
    -   **Railings**: Interrupted by aisles and tunnels.
    -   **Stairs**: Generated specifically within aisle zones.
    -   **Chairs**: Distributes chair geometry (Curves or Blocks) along the rows.
-   **Block Support**: Uses `AudienceSetup.PlanGeo` to place detailed block instances.

## Installation

1.  Build the solution `src/Owl.slnx` using Visual Studio 2022 or `dotnet build`.
2.  The build output will be in `src/Owl.Grasshopper/bin/Debug/net7.0/`.
3.  Copy `Owl.Grasshopper.gha` and `Owl.Core.dll` to your Grasshopper Libraries folder (usually `%APPDATA%\Grasshopper\Libraries`).
4.  Restart Rhino 8.

## Requirement

-   **Rhino 8** (Service Release 10 or later recommended).
-   **Grasshopper**.

## Usage

1.  **Define Section**: Use `Tribune Setup`, `Stair Setup`, `Railing Setup` components.
2.  **Define Audience**: Use `Audience Setup` to define eye heights and plan geometry.
3.  **Define Plan**: Use `Plan Setup` to define the boundary curve and aisle curves.
4.  **Solve**: Connect all setups to `Audience Distributor`.
5.  **Visualize**: The component outputs Analysis data, Sightlines, and Plan Geometry.
