using System.Drawing;
using GlmSharp;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    [PmdServerSync]
    public class Transform(PmdComponentConfig config, int initialX, int initialY) : PmdListenableComponent<Point>(config, new Point(initialX, initialY))
    {
        public bool IsTarget { get; set; }

        public vec2 ToVec2()
        {
            return new vec2(ComponentValue.X, ComponentValue.Y);
        }

        public void Set(vec2 vector)
        {
            SetValue(new Point((int)vector.x, (int)vector.y));
        }
    }

}
