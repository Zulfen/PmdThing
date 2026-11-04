using PmdThing.Core;
using PmdThing.Core.Hardware.Input;
using static PmdThing.Core.Util.PmdConstants;

namespace PmdThing.Components
{
    public class TiledInput : PmdComponent
    {
        private readonly TiledTransform _tiledTransform;

        public TiledInput(PmdComponentConfig config) : base(config)
        {
            _tiledTransform = GetParentComponent<TiledTransform>();
            Framework.Keyboard.KeyDown += KeyboardInput_KeyDown; 
        }

        private void KeyboardInput_KeyDown(PmdKey key)
        {
            if (_tiledTransform.Moving.Get()) return;

            switch (key)
            {
                case PmdKey.LeftArrow:
                    _tiledTransform.MoveInDirection(AnimationDirection.West);
                    break;
                case PmdKey.RightArrow:
                    _tiledTransform.MoveInDirection(AnimationDirection.East);
                    break;
                case PmdKey.DownArrow:
                    _tiledTransform.MoveInDirection(AnimationDirection.South);
                    break;
                case PmdKey.UpArrow:
                    _tiledTransform.MoveInDirection(AnimationDirection.North);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }

        public bool IsMovementKeyDown()
        {
            return Framework.Keyboard.IsKeyDown(PmdKey.LeftArrow)  ||
                   Framework.Keyboard.IsKeyDown(PmdKey.RightArrow) ||
                   Framework.Keyboard.IsKeyDown(PmdKey.DownArrow)  ||
                   Framework.Keyboard.IsKeyDown(PmdKey.UpArrow);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

    }
}
