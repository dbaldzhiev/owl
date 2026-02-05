using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class OWL_StairGenerator
    {
        public Rhino.Geometry.Curve Profile { get; private set; }
        public double RiserHeight { get; private set; }
        
        public List<Curve> Risers { get; private set; }
        public List<Curve> Treads { get; private set; }
        public List<string> Messages { get; private set; }

        public OWL_StairGenerator(Curve profile, double riserHeight)
        {
            if (profile == null || !profile.IsValid) throw new ArgumentException("Invalid profile curve.");
            if (riserHeight <= 0.001) throw new ArgumentException("Riser height must be greater than 0.");

            Profile = profile;
            RiserHeight = riserHeight;
            Risers = new List<Curve>();
            Treads = new List<Curve>();
            Messages = new List<string>();
        }

        public void Generate()
        {
            Risers.Clear();
            Treads.Clear();
            Messages.Clear();

            // Simple algorithm:
            // 1. Get bounding box Z range
            // 2. Start from bottom Z, step up by RiserHeight
            // 3. Intersect plane with curve to find step locations
            
            var bbox = Profile.GetBoundingBox(true);
            double zBottom = bbox.Min.Z;
            double zTop = bbox.Max.Z;

            // Align to integer multiples relative to start, or just start at bottom?
            // "Governed by constant riser height". Let's start at the exact zBottom.
            
            double currentZ = zBottom;
            Point3d? prevPt = null;

            // Create a plane for intersection
            Plane fitPlane = Plane.WorldXY; // Assuming profile is roughly in a vertical plane, checking Z cuts.
            // Actually, we don't know the orientation. But we assume Z is up.
            // We'll intersect using Plane(Point3d(0,0,z), Vector3d.ZAxis)
            
            int safety = 0;
            while (currentZ <= zTop && safety < 1000)
            {
                safety++;
                
                // Intersect curve with horizontal plane at currentZ
                var zPlane = new Plane(new Point3d(0, 0, currentZ), Vector3d.ZAxis);
                var events = Rhino.Geometry.Intersect.Intersection.CurvePlane(Profile, zPlane, 0.001);

                if (events != null && events.Count > 0)
                {
                    // Sort by something? Assuming 2D profile logic, usually specific direction.
                    // For now, let's take the first intersection point.
                    // Ideally we should sort by distance along curve or X coordinate.
                    Point3d hitPt = events[0].PointA;

                    if (prevPt.HasValue)
                    {
                        // Create Riser (Vertical from prev tread level to current)
                        // If this is the second iteration, we have a previous point which was at (currentZ - RiserHeight).
                        // Wait, geometry is:
                        // P1 at Z0. 
                        // P2 at Z1. 
                        // Tread connects P1_proj and P2? 
                        
                        // Standard stair:
                        // Vertical Riser UP from prevPt.
                        // Horizontal Tread from Riser Top to HitPt.
                        
                        Point3d riserBase = prevPt.Value;
                        Point3d riserTop = new Point3d(riserBase.X, riserBase.Y, currentZ);
                        
                        // Check if we actually went up (RiserHeight)
                        if (riserTop.DistanceTo(riserBase) > 0.001)
                        {
                            Risers.Add(new LineCurve(riserBase, riserTop));
                        }
                        
                        // Tread
                        Treads.Add(new LineCurve(riserTop, hitPt));
                    }
                    
                    prevPt = hitPt;
                }
                
                // Advance
                currentZ += RiserHeight;
            }
            
            // Handle last step if needed (to top)
            if (prevPt.HasValue && prevPt.Value.Z < zTop)
            {
                Point3d lastPt = Profile.PointAtEnd; // Crude approximation
                // Ideally project lastPt to final riser level?
            }
        }
    }
}
