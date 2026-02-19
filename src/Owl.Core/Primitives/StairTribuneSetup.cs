using System;

namespace Owl.Core.Primitives
{
    public class StairTribuneSetup
    {
        public TribuneSetup Tribune { get; set; }
        public StairSetup Stairs { get; set; }

        public StairTribuneSetup() { }

        public StairTribuneSetup(TribuneSetup tribune, StairSetup stairs)
        {
            Tribune = tribune ?? throw new ArgumentNullException(nameof(tribune));
            Stairs = stairs ?? throw new ArgumentNullException(nameof(stairs));
        }

        public StairTribuneSetup Duplicate()
        {
            return new StairTribuneSetup(Tribune.Duplicate(), Stairs.Duplicate());
        }
    }
}
