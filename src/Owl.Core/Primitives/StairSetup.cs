namespace Owl.Core.Primitives
{
    public class StairSetup
    {
        public double TreadHeight { get; }
        public double TreadWidth { get; }

        public StairSetup(double treadHeight, double treadWidth)
        {
            TreadHeight = treadHeight;
            TreadWidth = treadWidth;
        }

        public StairSetup Duplicate()
        {
            return new StairSetup(TreadHeight, TreadWidth);
        }
    }
}
