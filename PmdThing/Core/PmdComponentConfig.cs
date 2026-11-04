using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Network;
using PmdComponentId = System.Int32;

namespace PmdThing.Core
{
    public record PmdComponentConfig
    {
        public int Priority { get; init; }
        public required PmdGameBase Base { get; init; }
        public required PmdFramework Framework { get; init; }

        // can be null
        public PmdContainer? Parent = null;
    }
}
