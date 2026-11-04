using PmdThing.Core.Hardware.Framework;
using PmdComponentId = System.Int32;

namespace PmdThing.Core
{
    public record PmdComponentConfig
    {
        public PmdComponentId Priority { get; set; }
        public required PmdGameBase Base { get; set; }
        public required PmdFramework Framework { get; set; }

        // can be null
        public PmdContainer? Parent = null;
    }
}
