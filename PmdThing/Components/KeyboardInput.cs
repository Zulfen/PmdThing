using PmdThing.Core;
using PmdThing.Core.Hardware.Input;
using PmdThing.Core.Util;
using static PmdThing.Core.Util.PmdConstants;

namespace PmdThing.Components
{
    public class KeyboardInput : PmdComponent
    {
        private readonly TiledTransform _tiledTransform;

        public KeyboardInput(PmdComponentConfig config) : base(config)
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

        public void MoveTo(PmdTileRef tile)
        {
            
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

    }
}
