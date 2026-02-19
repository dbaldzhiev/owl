using System;
using Owl.Core.Primitives;

namespace Owl.Core.Primitives
{
    public class DisabledSeatsSetup
    {
        public AudienceSetup Setup { get; set; }
        public int Count { get; set; }
        public double Distribution { get; set; }

        public DisabledSeatsSetup()
        {
            Setup = new AudienceSetup();
            Count = 0;
            Distribution = 0.5;
        }

        public DisabledSeatsSetup(AudienceSetup setup, int count, double distribution)
        {
            Setup = setup ?? new AudienceSetup();
            Count = count;
            Distribution = distribution;
        }

        public DisabledSeatsSetup Duplicate()
        {
            return new DisabledSeatsSetup(Setup.Duplicate(), Count, Distribution);
        }
    }
}
