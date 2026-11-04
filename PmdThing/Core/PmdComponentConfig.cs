using PmdComponentId = System.Int32;

namespace PmdThing.Core
{
    public class PmdComponentConfig
    {
        public PmdComponentId Priority { get; set; }

        // can be null
        public PmdContainer? Parent = null;
    }
}
