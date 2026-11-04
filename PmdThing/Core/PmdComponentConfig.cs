using PmdThing.Core.Hardware.Framework;

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
