using System.Linq.Expressions;
using GlmSharp;
using PmdThing.Core;

namespace PmdThing.Components
{
    public class DebugDraw : PmdComponent
    {

        private readonly Transform _transform;

        public DebugDraw(PmdComponentConfig config) : base(config)
        {
            _transform = GetParentComponent<Transform>();
            DoesRendering = true;
        }

        public override void Render()
        {
            var position = _transform.GetPosition();
            var facing = _transform.GetDirection();
            Framework.Graphics.DrawLine(position.x, position.y, facing.x, facing.y);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
