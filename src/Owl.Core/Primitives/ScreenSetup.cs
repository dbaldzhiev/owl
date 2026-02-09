using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class ScreenSetup
    {
        public Curve ScreenCurve { get; set; }

        public ScreenSetup()
        {
        }

        public ScreenSetup(Curve screenCurve)
        {
            ScreenCurve = screenCurve;
        }

        public ScreenSetup Duplicate()
        {
            return new ScreenSetup(ScreenCurve?.DuplicateCurve());
        }
    }
}
