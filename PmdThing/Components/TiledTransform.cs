using System.Diagnostics;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    [PmdServerSync]
    public class TiledTransform : PmdListenableComponent<PmdTileRef>
    {
        public class DestinationEventArgs(PmdTileRef tile)
        {
            public PmdTileRef Tile { get; set; } = tile;
            public bool ShouldMove { get; set; } = true;
        }

        private readonly Timer _timer;

        private readonly PmdServerValue<PmdTileRef> _destination;

        public delegate Task AsyncDestinationChanged(PmdContainer? sender, DestinationEventArgs e);
        public event AsyncDestinationChanged? DestinationChanged;

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
            _destination = new PmdServerValue<PmdTileRef>(this, new PmdTileRef(0, 0), ProcessingOrder.Update, true, true);
            CanMove = new PmdServerValue<bool>(this, true);
            Moving = new PmdServerValue<bool>(this, false);
            Speed = new PmdServerValue<float>(this, 2f);

            Direction = new PmdServerValue<PmdConstants.AnimationDirection>(this,
                PmdConstants.AnimationDirection.South);

            RegisterSyncedIfNotExist(true, "transform", out _transform, 0, 0);

            // TODO: Account for multiple tiled worlds
            _world = GetBaseContainer<TiledWorld>();
            _timer = GetSharedComponent<Timer>();

            _destination.Changed += InternalDestination_Changed;

        }

        private async void InternalDestination_Changed(PmdTileRef current, PmdTileRef previous)
        {
            try
            {
                var eventArgs = new DestinationEventArgs(current);

                if (DestinationChanged != null)
                {
                    await DestinationChanged(GetParent(), eventArgs);

                    //Debugger.Break();

                    if (eventArgs.ShouldMove)
                    {
                        MoveTo(eventArgs.Tile);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void FaceTowards(PmdTileRef tile)
        {
            var tileDirection = tile.ToVec2() - ComponentValue.ToVec2();
            var animationDirection = PmdConstants.Vec2ToDirection(tileDirection);
            Direction.Set(animationDirection);
        }

        /// <summary>
        /// Request for a tile to be walked to.
        /// </summary>
        /// <param name="tile"></param>
        public void SetDestination(PmdTileRef tile)
        {
            FaceTowards(tile);
            _destination.Set(tile);
        }

        /// <summary>
        /// NOTE: doesn't take collision into account
        /// </summary>
        /// <param name="tile">Tile to move to</param>
        /// <param name="speed">Speed at which to do so</param>
        private void MoveTo(PmdTileRef tile, float speed = 2f)
        {

            Speed.Set(speed);
            Moving.Set(CanMove.Get());

            var destinationVector = _world
                .GetWorldSpace(tile)
                .ToVec2();

            // this sucks mega balls 
            var timerId = Register<Timer>(true, out var timer, 50f, ProcessingOrder.Update);
            timer.RegisterTask(() =>
            {
                var pointVector = _transform.ToVec2();
                var distanceVector = destinationVector - pointVector;
                var step = Speed.Get(); //* Framework.Graphics.GetDelta();

                if (distanceVector.Length <= step)
                {
                    SetValue(tile);
                }

                if (CanMove.Get() && distanceVector.Length > 0)
                {
                    var unitPos = distanceVector / distanceVector.Length;
                    _transform.Set(pointVector + unitPos * step);
                    return;
                }

                Moving.Set(false);
                DeregisterParent<Timer>(timerId, true);

            });
        }

        public void MoveInDirection(PmdConstants.AnimationDirection direction)
        {
            var animationDirection = PmdConstants.DirectionToVec2(direction);
            var newTile = PmdConstants.Vec2ToTile(ComponentValue.ToVec2() + animationDirection);
            SetDestination(newTile);
        }

        protected override void SetValue(PmdTileRef tile)
        {
            _transform.ComponentValue = _world.GetWorldSpace(tile);
            base.SetValue(tile);
        }

    }
}
