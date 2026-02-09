using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class OWL_TribuneProfileGenerator
    {
        public OWL_StairDefinition StairDefinition { get; private set; }
        public int RowCount { get; private set; }
        public double RowWidth { get; private set; }

        public PolylineCurve TribuneProfile { get; private set; }
        public List<PolylineCurve> StairProfiles { get; private set; }
        public double CalculatedRowHeight { get; private set; }

        public OWL_TribuneProfileGenerator(OWL_StairDefinition stairDefinition, int rowCount, double rowWidth)
        {
            StairDefinition = stairDefinition ?? throw new ArgumentNullException(nameof(stairDefinition));
            if (rowCount < 1) throw new ArgumentException("Row count must be at least 1.");
            if (rowWidth <= 0) throw new ArgumentException("Row width must be greater than 0.");

            RowCount = rowCount;
            RowWidth = rowWidth;
            StairProfiles = new List<PolylineCurve>();
        }

        public void Generate()
        {
            // 1. Calculate steps per row
            // We assume the row height is determined by the number of integer steps that fit in the width?
            // Or roughly? Let's use Round to find the closest number of steps that fit the RowWidth, 
            // but actually, "Stair Definition Input" implies the TREAD WIDTH is fixed.
            // So we fit N treads.
            
            int stepsPerRow = (int)Math.Max(1, Math.Round(RowWidth / StairDefinition.TreadWidth));
            CalculatedRowHeight = stepsPerRow * StairDefinition.TreadHeight;

            // 2. Generate Tribune Profile (The structural L-shapes)
            var tribunePts = new List<Point3d>();
            tribunePts.Add(Point3d.Origin); // Start at 0,0

            for (int i = 0; i < RowCount; i++)
            {
                double currentX = i * RowWidth;
                double currentZ = i * CalculatedRowHeight;

                // Bottom-Left of row is (currentX, currentZ)
                // Top-Right of row is (currentX + RowWidth, currentZ + CalculatedRowHeight)
                // Shape:
                // From (X, Z) -> (X + RowWidth, Z) -> (X + RowWidth, Z + RowHeight)
                // Wait, usually it's Rise then Run? Or Run then Rise?
                // Tribune: Sit on the flat part.
                // So (currentX, currentZ) -> (currentX + RowWidth, currentZ) (The floor)
                // then UP to next row level (currentX + RowWidth, currentZ + RowHeight)
                
                // Add flat part
                // If this is the start, we are at 0,0.
                // Line to (RowWidth, 0)
                // Line to (RowWidth, RowHeight)
                
                // Let's refine the points logic for a single Polyline
                // Use relative moves
                
                // For the tribune PROFILE, it's typically the "Plenum" or the "Steps". 
                // Let's assume it's the sequence of L-shapes.
                
                // Pt 1: Front edge of row i (currentX, currentZ) (Already added if i=0)
                // Pt 2: Back edge of row i (currentX + RowWidth, currentZ)
                tribunePts.Add(new Point3d(currentX + RowWidth, 0, currentZ));
                
                // Pt 3: Top of riser to next row (currentX + RowWidth, currentZ + RowHeight)
                // Only if not the last vertical? well, usually the profile includes the back wall of the last row.
                tribunePts.Add(new Point3d(currentX + RowWidth, 0, currentZ + CalculatedRowHeight));
            }

            TribuneProfile = new PolylineCurve(tribunePts);

            // 3. Generate Stair Profiles
            // One stair set per row (connecting row i to row i+1) embedded in the aisle
            StairProfiles.Clear();
            
            for (int i = 0; i < RowCount; i++)
            {
                // Stair starts at Row i level, and goes up to Row i+1 level
                // Origin for this stair segment:
                double startX = i * RowWidth;
                double startZ = i * CalculatedRowHeight;
                
                var stairPts = new List<Point3d>();
                stairPts.Add(new Point3d(startX, 0, startZ));

                for (int s = 0; s < stepsPerRow; s++)
                {
                    // For each step:
                    // Up (Riser) ? Or Forward (Tread)?
                    // Usually Start with Riser or Tread?
                    // If we start at (startX, startZ) which is the floor of the row.
                    // The first step goes UP (Riser) then FORWARD (Tread).
                    
                    double stepBaseX = startX + s * StairDefinition.TreadWidth;
                    double stepBaseZ = startZ + s * StairDefinition.TreadHeight;

                    // Riser UP
                    stairPts.Add(new Point3d(stepBaseX, 0, stepBaseZ + StairDefinition.TreadHeight));
                    
                    // Tread FORWARD
                    stairPts.Add(new Point3d(stepBaseX + StairDefinition.TreadWidth, 0, stepBaseZ + StairDefinition.TreadHeight));
                }
                
                // Verify alignment:
                // last point is at startX + steps * TreadWidth, startZ + steps * TreadHeight
                // This should match startX + RowWidth (approx) and startZ + RowHeight (exact)
                // Since stepsPerRow ~ RowWidth/TreadWidth, X might differ slightly. 
                // That's acceptable for "separate profiles", they might not perfectly match the tribune wall if widths don't align perfectly.
                
                StairProfiles.Add(new PolylineCurve(stairPts));
            }
        }
    }
}
