using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    [PmdServerSync]
    public class TiledTransform : PmdListenableComponent<PmdTileRef>
    {

        [PmdServerSync]
        public PmdListenableValue<PmdTileRef> Destination;

        [PmdServerSync]
        public PmdListenableValue<PmdConstants.AnimationDirection> Direction;

        private readonly Transform _transform;
        private readonly TiledWorld _world;

        public PmdListenableValue<bool> CanMove;

        public PmdListenableValue<bool> Moving;

        [PmdServerSync]
        public PmdListenableValue<float> Speed;

        public TiledTransform(PmdComponentConfig config, PmdTileRef tile) : this(config, tile.Row, tile.Column)
        {
        }

        public TiledTransform(PmdComponentConfig config, int column, int row) : base(config, new PmdTileRef(row, column))
        {
            Destination = new PmdListenableValue<PmdTileRef>(this, new PmdTileRef(0, 0));
            CanMove = new PmdListenableValue<bool>(this, true);
            Moving = new PmdListenableValue<bool>(this, false);
            Speed = new PmdListenableValue<float>(this, 2f);

            Direction = new PmdListenableValue<PmdConstants.AnimationDirection>(this,
                PmdConstants.AnimationDirection.South);

            RegisterIfNotExist(true, out _transform, 0, 0);

            // TODO: Account for multiple tiled worlds
            _world = GetBaseContainer<TiledWorld>();

            GetSharedComponent<Timer>()
               .RegisterTask(TiledPosition_FixedUpdate);

        }

        public void FaceTowards(PmdTileRef tile)
        {
            var tileDirection = tile.ToVec2() - ComponentValue.ToVec2();
            var animationDirection = PmdConstants.Vec2ToDirection(tileDirection);
            Direction.Set(animationDirection);
        }

        public void MoveTo(PmdTileRef tile, float speed = 2f)
        {
            FaceTowards(tile);
            Speed.Set(speed);

            Destination.Set(tile);
        }

        public void MoveInDirection(PmdConstants.AnimationDirection direction)
        {
            var animationDirection = PmdConstants.DirectionToVec2(direction);
            var newTile = PmdConstants.Vec2ToTile(ComponentValue.ToVec2() + animationDirection);
            MoveTo(newTile, 3f);
        }

        public void TiledPosition_FixedUpdate()
        {
            var destVector = _world.GetWorldSpace(Destination.Get());
            var distanceVector = destVector - _transform.ComponentValue;
            var step = Speed.Get(); //* Framework.Graphics.GetDelta();

            if (distanceVector.Length <= step && distanceVector.Length > 0 && Moving.Get())
            {
                _transform.ComponentValue = destVector;
                Moving.Set(false);

                ComponentValue = Destination.Get();
            }
            else if (CanMove.Get() && distanceVector.Length > 0)
            {
                if (!Moving.Get()) Moving.Set(true);

                var unitPos = distanceVector / distanceVector.Length;
                _transform.ComponentValue += unitPos * step;
            }
            else
            {
                Moving.Set(false);
            }
        }

        protected override void SetValue(PmdTileRef tile)
        {
            base.SetValue(tile);
            _transform.ComponentValue = _world.GetWorldSpace(tile);
        }

    }
}
