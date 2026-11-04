using PmdComponentId = System.Int32;

namespace PmdThing.Core
{
    public class PmdComponentConfig
    {
        public Func<PmdComponentId, PmdComponent>? OtherComponent { get; set; }
        public Func<Type, PmdComponentId, PmdComponent?>? ContainerComponent { get; set; }
        public Func<Type, PmdComponent>? SharedComponent { get; set; }
        public int Priority { get; set; }

        // can be null
        public PmdContainer? Parent = null;
    }
}
