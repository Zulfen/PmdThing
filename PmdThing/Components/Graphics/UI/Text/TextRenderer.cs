using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components.Graphics.UI.Text
{
    public class TextRenderer : PmdContainerComponent<TextContainer>
    {
        private readonly TextUiResources _resources;
        private readonly string _text;
        private readonly int _spacing;
        private readonly UiBounds _bounds;

        public TextRenderer(PmdComponentConfig config, string text, int spacing) : base(config)
        {
            DoesRendering = true;

            _resources = GetSharedComponent<TextUiResources>();
            _text = text;
            _spacing = spacing;
            _bounds = GetParentComponent<UiBounds>();
        }

        public override void Render()
        {
            // TODO: text wrapping
            _resources.DrawString(_text, _bounds.ComponentValue.X, _bounds.ComponentValue.Y, _spacing);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
