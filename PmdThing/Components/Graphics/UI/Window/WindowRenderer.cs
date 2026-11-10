using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components.Graphics.UI.Window
{
    public class WindowRenderer : PmdContainerComponent<Containers.Window>
    {
        private readonly UiBounds _bounds;
        private readonly WindowUiResources _resources;

        public WindowRenderer(PmdComponentConfig config) : base(config)
        {
            DoesRendering = true;

            _bounds = GetParentComponent<UiBounds>();
            if (_bounds.TileDimensions.Width < 3 || _bounds.TileDimensions.Height < 3)
            {
                throw new ArgumentException("WindowRenderer requires at least 3x3 tile dimensions.");
            }

            _resources = GetSharedComponent<WindowUiResources>();
        }

        public override void Render()
        {
            _resources.DrawWindow(_bounds);
        }
    }
}
