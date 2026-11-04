using System.ComponentModel;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Components.Networking.QueryHandlers
{
    public class TileQuery : QueryHandler
    {

        public record TileRequest
        {
            public RequestType Type { get; set; }
            public PmdTileRef? Tile { get; set; }
        }

        public enum RequestType
        {
            SheetLocation,
            Collision,
            TileWidth,
            TileHeight,
            Rows,
            Columns
        }

        private readonly TiledWorld _tiledWorld;

        public TileQuery(PmdComponentConfig config) : base(config)
        {
            _tiledWorld = GetBaseContainer<TiledWorld>();
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override dynamic? Handle(dynamic requestData)
        {
            
            switch (requestData.Type)
            {
                case RequestType.SheetLocation:

                    if (requestData.Tile != null)
                    {
                        return _tiledWorld.GetTileAt(requestData.Tile);
                    }

                    break;

                case RequestType.Collision:

                    if (requestData.Tile != null)
                    {
                        return _tiledWorld.TileProvider.CollisionCheck(requestData.Tile.Row, requestData.Tile.Column);
                    }
                    break;

                case RequestType.TileWidth:
                    return _tiledWorld.TileProvider.GetTileWidth();
                case RequestType.TileHeight:
                    return _tiledWorld.TileProvider.GetTileHeight();
                case RequestType.Rows:
                    return _tiledWorld.TileProvider.GetRows();
                case RequestType.Columns:
                    return _tiledWorld.TileProvider.GetColumns();
                default:
                    throw new InvalidEnumArgumentException();
            }

            return null;
        }

        public override Type GetDataType()
        {
            return typeof(TileRequest);
        }
    }
}
