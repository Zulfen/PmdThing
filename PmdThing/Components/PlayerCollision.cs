using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components
{

    public class PlayerCollision : PmdComponent
    {

        public record CollisionEventArgs(bool Collided, PmdTileRef Tile);

        private readonly TiledWorld _world;

        private readonly List<TiledTransform> _otherTiles = [];

        public event EventHandler<PmdContainer?, CollisionEventArgs>? CollisionChange;

        public PlayerCollision(PmdComponentConfig config, TiledTransform tiledTransform) : base(config)
        {
            tiledTransform.Destination.Changed += Destination_Changed;
            _world = GetBaseContainer<TiledWorld>();
        }

        private async void Destination_Changed(PmdTileRef current, PmdTileRef previous)
        {
            try
            {
                var worldColliding = await _world.TileProvider.CollisionCheck(current.Row, current.Column);
                var transformsColliding = false;

                PmdContainer? collidingContainer = null;
                foreach (var transform in _otherTiles)
                {
                    transformsColliding = current == transform.ComponentValue;
                    if (transformsColliding)
                    {
                        collidingContainer = transform.GetParent();
                        break;
                    }
                }

                var collided = worldColliding || transformsColliding;
                CollisionChange?.Invoke(worldColliding ? _world : collidingContainer, new CollisionEventArgs(collided, current));
            }
            catch (Exception)
            {
                // ignored
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
