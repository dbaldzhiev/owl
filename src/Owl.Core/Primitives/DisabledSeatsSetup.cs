using System;
using Owl.Core.Primitives;

namespace Owl.Core.Primitives
{
    public class DisabledSeatsSetup
    {
        public int Count { get; set; }
        public double Distribution { get; set; }

        public DisabledSeatsSetup()
        {
            Count = 0;
            Distribution = 0.5;
        }

        public DisabledSeatsSetup(int count, double distribution)
        {
            Count = count;
            Distribution = distribution;
        }

        public DisabledSeatsSetup Duplicate()
        {
            return new DisabledSeatsSetup(Count, Distribution);
        }
    }
}
