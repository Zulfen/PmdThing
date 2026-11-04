using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components.Graphics
{
    public class DrawableString : PmdContainerComponent<TextContainer>
    {
        private readonly DrawableTextHandler _handler;
        private readonly string _text;
        private readonly Transform _transform;

        public DrawableString(PmdComponentConfig config, string text) : base(config)
        {
            DoesRendering = true;

            _handler = ParentContainer.ParentContainer;
            _text = text;
            _transform = GetParentComponent<Transform>();
        }

        public override void Render()
        {
            _handler.DrawString(_text, _transform.ComponentValue.X, _transform.ComponentValue.Y);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
