using System;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class OWL_SeatOccupant
    {
        public Plane SeatPlane { get; private set; }
        public Point3d EyePoint { get; private set; }
        public Point3d HeadPoint { get; private set; }
        public Guid Id { get; private set; }

        public OWL_SeatOccupant(Plane seatPlane, double eyeHeight = 1.2, double headClearance = 0.12)
        {
            SeatPlane = seatPlane;
            Id = Guid.NewGuid();

            // Calculate Eye Point (Local Z up relative to plane)
            EyePoint = SeatPlane.Origin + SeatPlane.ZAxis * eyeHeight;

            // Calculate Head Point (Eye + clearance, usually strictly vertical or normal?)
            // Standard is Head is ABOVE Eye. 
            // If Eye is at 1.2, Head (top) is usually around 1.3-1.35. 
            // "Head Point" in TRD usually refers to the Raycast target or Clearance point.
            // TRD: "Head Point: Clearance point above seat reference (eye + offset)."
            
            HeadPoint = EyePoint + SeatPlane.ZAxis * headClearance;
        }

        public GeometryBase GetRepresentation()
        {
            // Simple stick figure or shapes
            var sphere = new Sphere(HeadPoint, 0.1);
            return sphere.ToBrep();
        }

        public override string ToString()
        {
            return $"Occupant {Id.ToString().Substring(0, 4)}";
        }
    }
}
