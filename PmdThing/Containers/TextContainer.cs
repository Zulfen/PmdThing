using System.Drawing;
using PmdThing.Components.Graphics.UI;
using PmdThing.Components.Graphics.UI.Text;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;

namespace PmdThing.Containers
{
    [PmdServerSync]
    public class TextContainer : PmdContainer
    {
        public TextContainer(PmdContainerConfig config, string text, int x, int y, int spacing) : base(config)
        {
            RegisterComponent<UiBounds>(true, new Point(x, y), new Size(), 16);
            if (!IsOnServer())
            {
                RegisterIfNotExist<TextUiResources>(false);
                RegisterComponent<TextRenderer>(true, out _, text, spacing);
            }
        }
    }
}
