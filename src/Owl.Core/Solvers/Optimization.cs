using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Core.Solvers
{
    public static class Optimization
    {
        /// <summary>
        /// Evaluates the fitness of a tribune layout.
        /// Higher is better.
        /// </summary>
        public static double Evaluate(
            TribuneSetup tribune,
            StairSetup stairs,
            RailingSetup railings,
            List<AudienceSetup> audiences,
            ScreenSetup screen,
            ProjectorSetup projector,
            bool flip = false,
            Point3d origin = default,
            List<bool> railingToggles = null,
            List<double> audienceOffsets = null
        )
        {
            // 1. Generate Geometry
            var solver = new TribuneSolver(tribune, stairs, railings);
            solver.Solve(
                out _, // tribuneProfile
                out _, // stairsProfile
                out _, // railingProfiles
                out _, // gaps
                out SerializedTribune serializedTribune,
                out _, // tribRows
                out _, // rrInt
                flip,
                origin,
                railingToggles,
                audiences
            );

            if (serializedTribune.RowPoints == null || serializedTribune.RowPoints.Count == 0)
                return -10000.0; // Invalid layout

            // 2. Analyze Sightlines & Projector
            Analysis.Calculate(
                audiences,
                serializedTribune,
                screen,
                projector,
                audienceOffsets,
                null, // PlanSetup
                out List<Line> sightlines,
                out _, // limitLines
                out Brep projectorCone,
                out _, // sectionChairs
                out _, // planChairs
                out _, // planTribune
                out _, // planRailings
                out _  // planStairs
            );

            // 3. Calculate Fitness
            double fitness = 0.0;
            
            // A) Sightline Quality (C-Value / Head Clearance)
            // Iterate rows starting from the second one (index 1)
            // We need to know the eye position of row i and row i-1 to check if i-1 blocks i.
            // Analysis.Calculate returns sightlines. sightlines[i] is the line from Eye[i] to ScreenBottom.
            
            // We need the ACTUAL eye points to check clearance. 
            // Analysis doesn't return them directly, but sightlines[i].From is the Eye point.
            
            // However, Analysis loops serializedTribune.RowPoints which might match audiences count or loop.
            // Let's assume sightlines list corresponds to rows in order.
            
            if (sightlines.Count > 1)
            {
                // We compare Row i (observer) looking over Row i-1 (blocker).
                for (int i = 1; i < sightlines.Count; i++)
                {
                    Line currentSightline = sightlines[i]; // Eye to Screen
                    Point3d currentEye = currentSightline.From;
                    
                    // The person in front is at index i-1.
                    // We need their 'Head Top' position.
                    // Usually Eye + some offset, or just Eye is 'Eye Level'.
                    // Standard C-Value measures clearance *over* the eye of the person in front + hat/hair.
                    // Let's assume Eye point acts as the blocker point for simplicity, or add standard offset (e.g. 12cm) if needed.
                    // For now, let's strictly check if the sightline passes ABOVE the previous eye.
                    
                    Point3d prevEye = sightlines[i-1].From;
                    
                    // Project prevEye onto the vertical plane of the sightline?
                    // Actually, simpler: measure vertical distance from prevEye to the line segment.
                    // But we want the vertical distance at the X/Y location of prevEye.
                    
                    // Find point on currentSightline that has same distance from screen as prevEye? 
                    // No, simpler: Drop a vertical line from prevEye and intersect with currentSightline.
                    
                    // Or: Distance from prevEye to Line (PointToLine), but signed?
                    // We want to know if prevEye is ABOVE or BELOW the line.
                    
                    // Let's use Z at the specific parameter.
                    // Project prevEye to line to get t?
                    // Be careful with 3D lines if they aren't parallel/planar. They should be roughly in XZ plane relative to each other if standard stadium.
                    // But if curved or offset, they might be skewed.
                    
                    // Robust way: closest point on line.
                    double t = currentSightline.ClosestParameter(prevEye);
                    Point3d ptOnLine = currentSightline.PointAt(t);
                    
                    // If ptOnLine.Z > prevEye.Z, then the sightline is ABOVE the head -> Good.
                    // If ptOnLine.Z < prevEye.Z, then the sightline is BELOW the head -> Bad (Blocked).
                    
                    double clearance = ptOnLine.Z - prevEye.Z;
                    
                    // Add to fitness.
                    // If clearance > 0: Reward (accumulate visibility).
                    // If clearance < 0: Penalty (blocked view).
                    
                    if (clearance < 0)
                    {
                        // Heavy penalty for blockage
                        fitness -= 1000.0 * Math.Abs(clearance); 
                    }
                    else
                    {
                        // Reward for clearance, but maybe diminishing returns or cap?
                        // Just linear for now.
                        fitness += clearance * 10.0;
                    }
                    
                    // Bonus: Capacity. More rows = more fitness?
                    // Each valid row adds some base score.
                    fitness += 100.0;
                }
            }
            
            // B) Projector Clearance
            if (projectorCone != null)
            {
                // Check if any audience member intersects the cone.
                // We'll approximate audience as a box around the Eye point or just the Eye point itself.
                // Or check the Chairs.
                // Let's check Eye points (sightlines.From) + maybe some head height.
                
                foreach (var line in sightlines)
                {
                    Point3d eye = line.From;
                    
                    // Check intersection with Projector Cone (Planar Brep)
                    // Since everything is likely on Y=0 plane, we check if the point is on/inside the Brep face.
                    // 'IsPointInside' works for solids. For a surface, we use ClosestPoint.
                    // If the closest point is near the Eye, it means the Eye is touching the light beam (bad).
                    
                    // Note: This assumes the 'Eye' is the top of the head/blocker.
                    // If the beam goes *through* the head, the closest point on the beam will be the Eye itself (dist ~ 0).
                    
                    Point3d closest = projectorCone.ClosestPoint(eye);
                    double dist = closest.DistanceTo(eye);
                    
                    if (dist < 0.05) // Tolerance: 5cm
                    {
                         // Check if the closest point is actually *on* the face, not just near the plane.
                         // Brep.ClosestPoint usually returns a point on the Brep (face or edge).
                         // So if distance is small, it IS on the Brep.
                         
                        fitness -= 50000.0; // Penalty for blocking projector
                    }
                }
            }

            return fitness;
        }
    }
}
