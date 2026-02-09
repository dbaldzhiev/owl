using System;

namespace Owl.Core.Primitives
{
    /// <summary>
    /// Defines the geometric properties of a stair iteration.
    /// </summary>
    public class OWL_StairDefinition
    {
        public double TreadHeight { get; private set; }
        public double TreadWidth { get; private set; }

        public OWL_StairDefinition(double treadHeight, double treadWidth)
        {
            if (treadHeight <= 0) throw new ArgumentException("Tread height must be greater than 0.");
            if (treadWidth <= 0) throw new ArgumentException("Tread width must be greater than 0.");

            TreadHeight = treadHeight;
            TreadWidth = treadWidth;
        }

        public override string ToString()
        {
            return $"Stair Def (H={TreadHeight:F3}, W={TreadWidth:F3})";
        }
    }
}
