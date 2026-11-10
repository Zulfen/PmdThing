using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components.Graphics
{
    public class DrawOrder(PmdComponentConfig config, float screenTopLeft) : PmdListenableComponent<float>(config, screenTopLeft);
}