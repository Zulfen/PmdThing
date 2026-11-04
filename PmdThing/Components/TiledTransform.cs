using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    [PmdServerSync]
    public class TiledTransform : PmdListenableComponent<PmdTileRef>
    {

        private readonly Timer _timer;

        public PmdServerValue<PmdTileRef> Destination;

        public PmdServerValue<PmdConstants.AnimationDirection> Direction;

        private readonly Transform _transform;
        private readonly TiledWorld _world;

        public PmdServerValue<bool> CanMove;

        public PmdServerValue<bool> Moving;

        public PmdServerValue<float> Speed;

        public TiledTransform(PmdComponentConfig config, PmdTileRef tile) : this(config, tile.Row, tile.Column)
        {
        }

        public TiledTransform(PmdComponentConfig config, int column, int row) : base(config, new PmdTileRef(row, column))
        {
            Destination = new PmdServerValue<PmdTileRef>(this, new PmdTileRef(0, 0));
            CanMove = new PmdServerValue<bool>(this, true);
            Moving = new PmdServerValue<bool>(this, false);
            Speed = new PmdServerValue<float>(this, 2f);

            Direction = new PmdServerValue<PmdConstants.AnimationDirection>(this,
                PmdConstants.AnimationDirection.South);

            RegisterSyncedIfNotExist(true, "transform", out _transform, 0, 0);

            // TODO: Account for multiple tiled worlds
            _world = GetBaseContainer<TiledWorld>();
            _timer = GetSharedComponent<Timer>();

        }

        public void FaceTowards(PmdTileRef tile)
        {
            var tileDirection = tile.ToVec2() - ComponentValue.ToVec2();
            var animationDirection = PmdConstants.Vec2ToDirection(tileDirection);
            Direction.Set(animationDirection);
        }

        public void MoveTo(PmdTileRef tile, float speed = 2f)
        {
            if (Moving.Get()) return;

            FaceTowards(tile);
            Speed.Set(speed);

            Destination.Set(tile);
            Moving.Set(true);

            // this sucks mega balls 
            var timerId = Register<Timer>(true, out var timer, (double)1000 / 30, ProcessingOrder.Update);
            timer.RegisterTask(() =>
            {
                var pointVector = _transform.ToVec2();
                var distanceVector = _world.GetWorldSpace(tile).ToVec2() - pointVector;
                var step = Speed.Get(); //* Framework.Graphics.GetDelta();

                if (distanceVector.Length <= step)
                {
                    Moving.Set(false);

                    SetValue(tile);
                    Destination.Set(tile);

                    DeregisterParent<Timer>(timerId, true);
                }
                else if (CanMove.Get() && distanceVector.Length > 0)
                {
                    var unitPos = distanceVector / distanceVector.Length;
                    _transform.Set(pointVector + unitPos * step);
                }
            });
        }

        public void MoveInDirection(PmdConstants.AnimationDirection direction)
        {
            var animationDirection = PmdConstants.DirectionToVec2(direction);
            var newTile = PmdConstants.Vec2ToTile(ComponentValue.ToVec2() + animationDirection);
            MoveTo(newTile, 3f);
        }

        protected override void SetValue(PmdTileRef tile)
        {
            _transform.ComponentValue = _world.GetWorldSpace(tile);
            base.SetValue(tile);
        }

    }
}
