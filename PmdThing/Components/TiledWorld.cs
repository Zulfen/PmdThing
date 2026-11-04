using GlmSharp;
using PmdThing.Core;
using PmdThing.Core.Util;
namespace PmdThing.Components
{
    public class TiledWorld : PmdComponent
    {

        private readonly SpriteSheet _tileSheet;
        public int WorldTilesId;

        private readonly int _tileSize;

        private readonly int[,] _tiles = new int[50, 50];


        private readonly Dictionary<int, Transform> _tiledSpriteMap = [];

        private int _tileSpaceLimitX;
        private int _tileSpaceLimitY;

        private int _screenSpaceX;
        private int _screenSpaceY;

        public TiledWorld(PmdComponentConfig config, int tileSize) : base(config)
        {
            _tileSize = tileSize;

            var window = Framework.Graphics.GetMainWindow();
            _tileSpaceLimitX = window.GetWidth() / _tileSize;
            _tileSpaceLimitY = window.GetHeight() / _tileSize;

            WorldTilesId = RegisterIfNotExist
            (
                false,
                out _tileSheet,
                @"C:\Users\Joshua Passmore\source\repos\PmdThing\PmdThing\TileSets\tileset142_dpc.png",
                _tileSize,
                _tileSize
            );

            for (var row = 0; row < _tiles.GetLength(0); row++)
            {
                _tiles[row, 0] = 9;
                _tiles[row, 1] = 3;
            }

            ShouldUpdate = true;
            DoesRendering = true;

        }

        private void TiledWorld_SpriteTileChanged(PmdTileRef newTile, PmdTileRef oldTile)
        {
            var newIndex = newTile.Row + newTile.Column;
            var oldIndex = oldTile.Row + oldTile.Column;

            if (_tiledSpriteMap.Remove(oldIndex, out var position))
            {
                _tiledSpriteMap[newIndex] = position;
            }
        }


        public void AddPositionComponent(Transform transform, TiledTransform tiledTransform)
        {
            //var index = tiledTransform.Value.Column + tiledTransform.Value.Row;
            //tiledTransform.Changed += TiledWorld_SpriteTileChanged;
            //_tiledSpriteMap.Add(index, transform);
        }

        public void AddPositionComponent(int positionId, int tiledPositionId)
        {
            var position = GetOtherComponent<Transform>(positionId);
            var tiledPosition = GetOtherComponent<TiledTransform>(positionId);

            AddPositionComponent(position, tiledPosition);
        }

        public void AddContainer(PmdContainer container)
        {
            var position = container.GetComponent<Transform>();
            var tiledPosition = container.GetComponent<TiledTransform>();

            AddPositionComponent(position, tiledPosition);
        }

        public int GetTileSize()
        {
            return _tileSize;
        }

        public override void Update()
        {
            //var screenSpaceY = 0;
            //var screenSpaceX = 0;

            //for (var row = 0; row < _tiles.GetLength(0); row++)
            //{
            //    for (var column = 0; column < _tiles.GetLength(1); column++)
            //    {
            //        var index = row + column;
            //        if (_tiledSpriteMap.TryGetValue(index, out var position))
            //        {
            //            if (position.GetChanged()) position.SetPosition(screenSpaceX, screenSpaceY);
            //            return;
            //        }

            //        screenSpaceX += _tileSheet.GetScaledSSpriteGetSize();

            //    }

            //    screenSpaceY += _tileSheet.GetScaledSpriteGetSize();

            //}
        }

        public vec2 GetScreenSpace(PmdTileRef tile)
        {
            return new vec2(
                tile.Column * _tileSheet.GetScaledSpriteGetSize(),
                tile.Row * _tileSheet.GetScaledSpriteGetSize()
            );
        }

        public override void Render()
        {

            var screenSpaceY = 0;
            var screenSpaceX = 0;

            for (var row = 0; row < _tiles.GetLength(0); row++)
            {
                for (var column = 0; column < _tiles.GetLength(1); column++)
                {
                    var sheetColumn = _tiles[row, 0];
                    var sheetRow = _tiles[row, 1];

                    _tileSheet.DrawSprite(screenSpaceX, screenSpaceY, sheetRow, sheetColumn, false);

                    screenSpaceX += _tileSheet.GetScaledSpriteGetSize();
                }

                screenSpaceX = 0;
                screenSpaceY += _tileSheet.GetScaledSpriteGetSize();

            }

        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
