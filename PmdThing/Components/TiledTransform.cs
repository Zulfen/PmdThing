using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    public class TiledTransform : PmdListenableComponent<PmdTileRef>
    {

        public PmdListenableValue<PmdTileRef> Destination;
        private readonly PmdListenableValue<PmdConstants.AnimationDirection> _direction;

        private readonly Transform _transform;
        private readonly TiledWorld _tiledWorld;
        private readonly Animation? _animation;

        public PmdListenableValue<bool> Moving;
        private float _speed;

        public TiledTransform(PmdComponentConfig config, int column, int row) : base(config, new PmdTileRef(row, column))
        {
            Destination = new PmdListenableValue<PmdTileRef>(this, new PmdTileRef(0, 0));
            Moving = new PmdListenableValue<bool>(this, false);

            _direction = new PmdListenableValue<PmdConstants.AnimationDirection>(this,
                PmdConstants.AnimationDirection.South);

            _transform = GetParentComponent<Transform>();
            _tiledWorld = GetSharedComponent<TiledWorld>();
            _animation = GetParentIfExists<Animation>();

            SetPosition(Value);

            GetSharedComponent<Timer>()
               .RegisterTask(TiledPosition_FixedUpdate);

            Moving.Changed += TiledTransform_MovingChanged;

        }

        private void TiledTransform_MovingChanged(bool oldValue, bool newValue)
        {
            if (_animation != null)
            {
                _animation.CurrentAnimation = newValue ? PmdConstants.AnimationType.Walk : PmdConstants.AnimationType.Idle;
            }
        }

        public void FaceTowards(PmdTileRef tile)
        {
            var tileVector = tile.ToVec2();

            var tileDirection = tileVector - Value.ToVec2();
            var animationDirection = PmdConstants.Vec2ToDirection(tileDirection);
            _direction.Set(animationDirection);

            _animation?.ChangeDirection(animationDirection);
        }

        public void MoveTo(PmdTileRef tile, float speed = 1.5f)
        {
            FaceTowards(tile);
            _speed = speed;

            Destination.Set(tile);

            Moving.Set(true);

        }

        public void MoveInDirection(PmdConstants.AnimationDirection direction)
        {
            var animationDirection = PmdConstants.DirectionToVec2(direction);
            var currentPos = Value.ToVec2();
            var newTile = PmdConstants.Vec2ToTile(currentPos + animationDirection);
            MoveTo(newTile, 2);
        }

        public void TiledPosition_FixedUpdate()
        {
            var destVector = _tiledWorld.GetScreenSpace(Destination.Get());
            var distanceVector = destVector - _transform.GetPosition();
            var step = _speed * Framework.Graphics.GetDelta();
            if (distanceVector.Length <= step)
            {
                _transform.SetPosition(destVector);
                Moving.Set(false);
                Value = Destination.Get();
            }
            else
            {
                var currentPos = _transform.GetPosition();
                var newPos = distanceVector / distanceVector.Length;
                _transform.SetPosition(currentPos + newPos * step);
            }

        }

        public void SetPosition(PmdTileRef tile)
        {
            Value = tile;
            _transform.SetPosition(_tiledWorld.GetScreenSpace(tile));
        }

    }
}
