using System;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    /// <summary>
    /// The primary primitive for Owl. Defines the controlling geometric logic of a tribune.
    /// Can be straight or curved. Defines the horizontal progression of rows.
    /// </summary>
    public class OWL_TribuneLine
    {
        public Curve BaseCurve { get; private set; }
        
        public bool IsValid => BaseCurve != null && BaseCurve.IsValid;

        public OWL_TribuneLine(Curve curve)
        {
            if (curve == null) throw new ArgumentNullException(nameof(curve));
            BaseCurve = curve.DuplicateCurve();
        }

        public override string ToString()
        {
            return $"Owl Tribune Line (L={BaseCurve.GetLength():F2})";
        }
    }
}
