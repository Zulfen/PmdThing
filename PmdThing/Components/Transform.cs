using GlmSharp;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    public class Transform : PmdComponent
    {

        private readonly PmdListenableValue<vec2> _position;
        private readonly PmdListenableValue<vec2> _facing;

        private float _speed;

        public Transform(PmdComponentConfig config, int initialX, int initialY) : base(config)
        {
            _position = new PmdListenableValue<vec2>(this, new vec2(initialX, initialY));
            _facing = new PmdListenableValue<vec2>(this, new vec2(0, 0));
            _speed = 0;
        }

        public void SetPosition(vec2 newPosition)
        {
            _position.Set(newPosition);
        }

        public void FaceTowards(vec2 newPosition)
        {
            var newFacing = newPosition - _position.Get();
            _facing.Set(newFacing.Normalized);
        }

        public vec2 GetPosition()
        {
            return _position.Get();
        }

        public vec2 GetDirection()
        {
            return _facing.Get();
        }

        public void ListenChanged(Action<vec2, vec2> handler)
        {
            _position.Changed += handler;
        }

        public void ListenDirectionChanged(Action<vec2, vec2> handler)
        {
            _facing.Changed += handler;
        }

        public bool GetChanged()
        {
            return _position.GetChanged();
        }

        public override void Cleanup()
        {
            // nothing
        }
    }

}
