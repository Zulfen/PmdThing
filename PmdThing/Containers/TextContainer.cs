using PmdThing.Components;
using PmdThing.Components.Graphics;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Containers
{
    [PmdServerSync]
    public class TextContainer : PmdNestedContainer<DrawableTextHandler>
    {
        public TextContainer(PmdContainerConfig config, string text, int x, int y) : base(config)
        {
            RegisterSyncedComponent<Transform>(true, out _, "transform", x, y);
            RegisterComponent<DrawableString>(true, out _, text);
        }
    }
}
