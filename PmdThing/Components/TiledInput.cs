using PmdThing.Components.Networking;
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
            
        }

        public override void Update()
        {
            if (IsMovementKeyDown(out var key))
            {
                if (_tiledTransform.Moving.Get()) return;

                switch (key)
                {
                    case PmdKey.LeftArrow:
                        MoveInDirection(AnimationDirection.West);
                        break;
                    case PmdKey.RightArrow:
                        MoveInDirection(AnimationDirection.East);
                        break;
                    case PmdKey.DownArrow:
                        MoveInDirection(AnimationDirection.South);
                        break;
                    case PmdKey.UpArrow:
                        MoveInDirection(AnimationDirection.North);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(key), key, null);
                }
            }
        }

        private void MoveInDirection(AnimationDirection direction)
        {
            // checks for if it's in a client / server environment, as it is optional!
            if (Client == null)
            {
                _tiledTransform.MoveInDirection(direction);
            }
            else
            {
                Client.SendRequest(QueryType.InputTileMove, GetParent(), direction);
            }
        }

        public bool IsMovementKeyDown(out PmdKey? key)
        {
            if (Framework.Keyboard.IsKeyDown(PmdKey.LeftArrow))
            {
                key = PmdKey.LeftArrow;
                return true;
            }
            if (Framework.Keyboard.IsKeyDown(PmdKey.RightArrow))
            {
                key = PmdKey.RightArrow;
                return true;
            }
            if (Framework.Keyboard.IsKeyDown(PmdKey.DownArrow))
            {
                key = PmdKey.DownArrow;
                return true;
            }   
            if (Framework.Keyboard.IsKeyDown(PmdKey.UpArrow))
            {
                key = PmdKey.UpArrow;
                return true;
            }

            key = null;
            return false;
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

    }
}
