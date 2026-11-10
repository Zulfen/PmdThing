using PmdThing.Components.Graphics.UI;
using PmdThing.Components.Graphics.UI.Window;
using PmdThing.Core;
using System.Drawing;
using PmdThing.Core.Network.Rokon;
using PmdContainerId = int;

namespace PmdThing.Containers
{
    [PmdServerSync]
    public class Window : PmdMultiContainer
    {

        private readonly UiBounds _bounds;

        public Window(PmdContainerConfig config, int x, int y, int size) : this(config, new Point(x, y), new Size(size, size))
        {
        }

        public Window(PmdContainerConfig config, Point screenTopLeft, Size tileDimensions) : base(config)
        {
            RegisterComponent<UiBounds>(true, out var bounds, screenTopLeft, tileDimensions, 16);
            _bounds = bounds;

            if (!IsOnServer())
            {
                RegisterIfNotExist<WindowUiResources>(false);
                RegisterComponent<WindowRenderer>(true);
            }

            ContainerRegistered += Window_ContainerRegistered;
        }

        private void Window_ContainerRegistered(PmdContainerId id, PmdContainer container, object[]? args)
        {
            if (container is TextContainer)
            {
                var textBounds = container.GetComponent<UiBounds>();
                textBounds.ScreenTopLeft = new Point(_bounds.ScreenTopLeft.X + textBounds.ScreenTopLeft.X, _bounds.ScreenTopLeft.Y + textBounds.ScreenTopLeft.Y);
            }
        }
    }
}
