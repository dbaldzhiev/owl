using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class OWL_SeatRow
    {
        public Curve RowCurve { get; private set; }
        public double SeatWidth { get; private set; }
        public List<OWL_SeatOccupant> Occupants { get; private set; }

        public OWL_SeatRow(Curve curve, double seatWidth, double eyeHeight, double headClearance)
        {
            if (curve == null || !curve.IsValid) throw new ArgumentException("Invalid row curve");
            RowCurve = curve;
            SeatWidth = seatWidth;
            Occupants = new List<OWL_SeatOccupant>();

            Generate(eyeHeight, headClearance);
        }

        private void Generate(double eyeHeight, double headClearance)
        {
            // Simple division for now.
            // In reality, we need complex spacing logic (Center justification, etc).
            // V0.1: Divide by length.
            
            double len = RowCurve.GetLength();
            int count = (int)(len / SeatWidth);
            
            if (count < 1) return;

            // Centering logic:
            // Total width of seats = count * SeatWidth
            // Remainder = len - Total width
            // Start offset = Remainder / 2 + (SeatWidth / 2) -> Center of first seat
            
            double remainder = len - (count * SeatWidth);
            double startDist = (remainder / 2.0) + (SeatWidth / 2.0);

            for (int i = 0; i < count; i++)
            {
                double dist = startDist + (i * SeatWidth);
                // Calculate parameter at length first
                double t;
                if (RowCurve.LengthParameter(dist, out t))
                {
                    Point3d pt = RowCurve.PointAt(t);
                    
                    // Get tangent for orientation
                    Vector3d tangent = RowCurve.TangentAt(t);
                    
                    // Construct Seat Plane
                    // Z is World Z? Or Curve Normal? 
                    // Usually Auditoriums are World Z oriented.
                    Vector3d up = Vector3d.ZAxis;
                    Vector3d right = Vector3d.CrossProduct(tangent, up); // Facing forward? 
                    // If curve goes Left->Right, Tangent is X+. Up is Z+. Cross(X, Z) = -Y. 
                    // Viewers usually face -Y (towards screen at 0,0).
                    // Let's assume Tangent is "Right" of the seated person (Row direction).
                    // Then Forward is Cross(Up, Tangent).
                    
                    Vector3d forward = Vector3d.CrossProduct(up, tangent);
                    
                    Plane seatPlane = new Plane(pt, tangent, forward); 
                    // Plane(Origin, XAxis, YAxis). X=Tangent (Row dir), Y=Forward. Z=Up.
                    
                    Occupants.Add(new OWL_SeatOccupant(seatPlane, eyeHeight, headClearance));
                }
            }
        }
    }
}
