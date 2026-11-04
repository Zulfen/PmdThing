using Microsoft.Extensions.Logging;
using PmdThing.Components;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Containers
{

    public class TiledWorld : PmdMultiContainer
    {

        // container lookup
        private readonly Dictionary<PmdTileRef, int> _tileIds = [];

        public IPmdTileProvider TileProvider { get; init; }

        public TiledWorld(IPmdTileProvider tileProvider)
        {
            TileProvider = tileProvider;

            for (var row = 0; row < TileProvider.GetRows(); row++)
            {
                for (var column = 0; column < TileProvider.GetColumns(); column++)
                {
                    _tileIds.Add(new PmdTileRef(row, column), RegisterEmptyContainer(out _));
                }
            }

            
            RegisterComponent<TiledWorldRenderer>(false);
            
        }

        public PmdContainer? GetTileAt(PmdTileRef tile)
        {
            return _tileIds.TryGetValue(tile, out var id) ? GetContainer<PmdContainer>(id) : null;
        }

        public PmdContainer? GetTileAt(int row, int column)
        {
            return GetTileAt(new PmdTileRef(row, column));
        }

        public List<PmdContainer> GetAllTiles()
        {
            return _tileIds.Values
                .Select(GetContainer<PmdContainer>)
                .ToList();
        }

    }

}
