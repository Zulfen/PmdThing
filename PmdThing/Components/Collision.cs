using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components
{

    public class Collision : PmdComponent
    {

        private readonly TiledTransform _transform;
        private readonly TiledWorld _world;

        private readonly List<TiledTransform> _otherTiles = [];

        public event Action<bool, PmdTileRef>? CollisionChange;

        public Collision(PmdComponentConfig config, TiledTransform tiledTransform) : base(config)
        {
            _transform = tiledTransform;
            _transform.Destination.Changed += Destination_Changed;

            _world = GetBaseContainer<TiledWorld>();
        }

        private async void Destination_Changed(PmdTileRef current, PmdTileRef previous)
        {
            var worldColliding = await _world.TileProvider.CollisionCheck(current.Row, current.Column);
            var transformsColliding = false;

            foreach (var transform in _otherTiles)
            {
                transformsColliding = current == transform.ComponentValue;
            }

            var collided = worldColliding || transformsColliding;
            CollisionChange?.Invoke(collided, current);
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
