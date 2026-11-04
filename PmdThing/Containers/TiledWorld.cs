using GlmSharp;
using PmdThing.Components;
using PmdThing.Core;
using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Util;

namespace PmdThing.Containers
{

    public class TiledWorld : PmdMultiContainer
    {

        // container lookup
        private int[,] _tileIds = new int[0,0];
        public IPmdTileProvider TileProvider { get; init; }
        private readonly TiledWorldRenderer? _renderer;

        private readonly bool _headless;

        private readonly TaskCompletionSource<int> _tileWidthSrc = new();
        private readonly TaskCompletionSource<int> _tileHeightSrc = new();

        public TiledWorld(PmdContainerConfig config, IPmdTileProvider tileProvider) : base(config)
        {
            TileProvider = tileProvider;
            AsyncInit = true;

            _headless = Framework.Graphics
                .GetCapabilities()
                .Headless;

            if (!_headless)
            {
                RegisterComponent<TiledWorldRenderer>(true, out var renderer);
                _renderer = renderer;
            }
        }

        public override async Task Init()
        {
            var rows = await TileProvider.GetRows();
            var columns = await TileProvider.GetColumns();

            _tileWidthSrc.SetResult(await TileProvider.GetTileWidth());
            _tileHeightSrc.SetResult(await TileProvider.GetTileHeight());

            _tileIds = new int[rows, columns];
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    _tileIds[row, column] = RegisterEmptyContainer(out _);

                    var tile = GetTileAt(row, column);
                    var imageRect = await TileProvider.GetSheetLocation(row, column);
                    tile?.RegisterProperty(imageRect);
                }
            }

            var imagePath = await TileProvider.GetImagePath();

            if (!_headless)
            {
                _renderer?.SetData(rows, columns, imagePath);
            }
        }

        public PmdContainer? GetTileAt(PmdTileRef tile)
        {
            if (tile.Row >= 0 && tile.Row < _tileIds.GetLength(0) && tile.Column >= 0 && tile.Column < _tileIds.GetLength(1))
            {
                return GetContainer(_tileIds[tile.Row, tile.Column]);
            }
            return null;
        }

        public PmdContainer? GetTileAt(int row, int column)
        {
            return GetTileAt(new PmdTileRef(row, column));
        }

        public List<PmdContainer> GetAllTiles()
        {
            var tiles = new List<PmdContainer>();
            for (var row = 0; row < _tileIds.GetLength(0); row++)
            {
                for (var column = 0; column < _tileIds.GetLength(1); column++)
                {
                    var container = GetContainer(_tileIds[row, column]);
                    tiles.Add(container);
                }
            }

            return tiles;
        }

        public vec2 GetWorldSpace(PmdTileRef tile)
        {
            return new vec2(
                tile.Column * _tileWidthSrc.Task.Result * SpriteSheet.SpriteScale,
                tile.Row * _tileHeightSrc.Task.Result * SpriteSheet.SpriteScale
            );
        }

    }

}
