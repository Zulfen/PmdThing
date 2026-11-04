using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components
{

    public class PlayerCollision : PmdComponent
    {

        public record CollisionEventArgs(PmdTileRef Tile);

        private readonly TiledWorld _world;

        private TiledTransform _transform;
        private readonly List<TiledTransform> _otherTiles = [];

        public event EventHandler<PmdContainer?, CollisionEventArgs>? CollisionCheck;

        public PlayerCollision(PmdComponentConfig config, TiledTransform tiledTransform) : base(config)
        {
            _transform = tiledTransform;
            _transform.DestinationChanged += Destination_Changed;
            _world = GetBaseContainer<TiledWorld>();
        }

        private async Task Destination_Changed(PmdContainer? sender, TiledTransform.DestinationEventArgs e)
        {

            var worldColliding = await _world.TileProvider.CollisionCheck(e.Tile.Row, e.Tile.Column);
            var transformsColliding = false;

            PmdContainer? collidingContainer = null;
            foreach (var transform in _otherTiles)
            {
                transformsColliding = e.Tile == transform.ComponentValue;
                if (transformsColliding)
                {
                    collidingContainer = transform.GetParent();
                    break;
                }
            }

            var collided = worldColliding || transformsColliding;
            if (collided)
            {
                CollisionCheck?.Invoke(worldColliding ? _world : collidingContainer,
                    new CollisionEventArgs(e.Tile));

                e.ShouldMove = false;
            }

        }
        public void RegisterTransform(TiledTransform transform)
        {
            _otherTiles.Add(transform);
        }

        public override Type? TickBefore()
        {
            return typeof(TiledTransform);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

    }
}
