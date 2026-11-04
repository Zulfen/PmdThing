using GlmSharp;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    [PmdServerSync]
    public class Transform(PmdComponentConfig config, int initialX, int initialY) : PmdListenableComponent<vec2>(config, new vec2(initialX, initialY))
    {
        public bool IsTarget { get; set; }
    }

}
