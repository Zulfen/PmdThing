using DotTiled;
using DotTiled.Serialization;
using System.Drawing;

namespace PmdThing.Core.Util
{
    public class TmxTileProvider : IPmdTileProvider
    {

        private readonly Map _tmxMap;

        private TileLayer _tileLayer;
        private string _visualLayer = string.Empty;
        public string VisualLayer
        {
            get => _visualLayer;
            set
            {
                _visualLayer = value;
                _tileLayer = GetTileLayer(_visualLayer);
            }
        }

        private ObjectLayer _objectLayer;
        private string _collisionLayer = string.Empty;
        public string CollisionLayer
        {
            get => _collisionLayer;
            set
            {
                _collisionLayer = value;
                _objectLayer = GetObjectLayer(_collisionLayer);
            }
        }

        private Tileset _tileset;
        private string _tileSetName = string.Empty;
        public string TilesetName
        {
            get => _tileSetName;
            set
            {
                _tileSetName = value;
                _tileset = GetTileset(_tileSetName);
            }
        }

        public TmxTileProvider
        (
            string tmxFilePath,
            string visualLayer = "Visuals",
            string collisionLayer = "Collision",
            string tileset = "testing"
        )
        {
            var loader = Loader.Default();
            _tmxMap = loader.LoadMap(tmxFilePath);

            _tileLayer = GetTileLayer(visualLayer);
            _objectLayer = GetObjectLayer(collisionLayer);
            _tileset = GetTileset(tileset); 
        }

        public Task<string> GetImagePath()
        {
            return Task.FromResult(Path.Join(Directory.GetCurrentDirectory(), "TileSets", _tileset.Image.Value.Source.Value));
        }

        /// <summary>
        /// Get rectangle of tile's texture in the sprite sheet.
        /// </summary>
        /// <param name="row">Row</param>
        /// <param name="column">Column</param>
        /// <returns>Rectangle of location in sprite sheet</returns>
        public Task<RectangleF> GetSheetLocation(int row, int column)
        {
            var tileGid = _tileLayer.GetGlobalTileIDAtCoord(column, row);
            var tileset = _tmxMap.ResolveTilesetForGlobalTileID(tileGid, out var localTileId);
             
            var sourceRect = tileset.GetSourceRectangleForLocalTileID(localTileId);

            return Task.FromResult(new RectangleF
            {
                X = sourceRect.X,
                Y = sourceRect.Y,
                Width = sourceRect.Width,
                Height = sourceRect.Height
            });
        }

        public Task<bool> CollisionCheck(int row, int column)
        {

            var posRect = new RectangleF()
            {
                X = column * _tmxMap.TileWidth,
                Y = row * _tmxMap.TileHeight,
                Width = _tmxMap.TileWidth,
                Height = _tmxMap.TileHeight
            };

            foreach (var collisionRect in _objectLayer.Objects.OfType<RectangleObject>())
            {
                var mapRect = new RectangleF()
                {
                    X = collisionRect.X,
                    Y = collisionRect.Y,
                    Width = (int)collisionRect.Width,
                    Height = (int)collisionRect.Height,
                };

                if (posRect.IntersectsWith(mapRect)) return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private TileLayer GetTileLayer(string name)
        {
            return _tmxMap.Layers
                .OfType<TileLayer>()
                .SingleOrDefault(layer => layer.Name == name) ?? throw new ArgumentException("Layer could not be found");
        }

        private ObjectLayer GetObjectLayer(string name)
        {
            return _tmxMap.Layers
                .OfType<ObjectLayer>()
                .SingleOrDefault(layer => layer.Name == name) ?? throw new ArgumentException("Layer could not be found");
        }

        private Tileset GetTileset(string name)
        {
            return _tmxMap.Tilesets
                .SingleOrDefault(tileset => tileset.Name == name) ?? throw new ArgumentException("Tileset could not be found");
        }

        public Task<int> GetTileWidth()
        {
            return Task.FromResult(_tmxMap.TileWidth);
        }

        public Task<int> GetTileHeight()
        {
            return Task.FromResult(_tmxMap.TileHeight);
        }

        public Task<int> GetRows()
        {
            return Task.FromResult(_tmxMap.Height);
        }

        public Task<int> GetColumns()
        {
            return Task.FromResult(_tmxMap.Width);
        }

    }
}
