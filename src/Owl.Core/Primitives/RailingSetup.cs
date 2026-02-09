namespace Owl.Core.Primitives
{
    public class RailingSetup
    {
        public double RailHeight { get; }
        public double RailWidth { get; }

        public RailingSetup(double railHeight, double railWidth)
        {
            RailHeight = railHeight;
            RailWidth = railWidth;
        }
    }
}
