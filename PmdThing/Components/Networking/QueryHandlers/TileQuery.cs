using System.ComponentModel;
using System.Drawing;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Network;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components.Networking.QueryHandlers
{
    public class TileQuery : QueryHandler
    {

        [PmdSerializable]
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
            Columns,
            Path
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

        public override dynamic? Handle(dynamic? requestData, IPmdServerConnection connection)
        {
            
            switch (requestData?.Type)
            {
                case RequestType.SheetLocation:

                    if (requestData.Tile != null)
                    {
                        PmdContainer? tileContainer = _tiledWorld.GetTileAt(requestData.Tile);
                        return tileContainer.GetProperty<RectangleF>();
                    }

                    break;

                case RequestType.Collision:

                    if (requestData.Tile != null)
                    {
                        return _tiledWorld.TileProvider
                            .CollisionCheck(requestData.Tile.Row, requestData.Tile.Column)
                            .Result;
                    }
                    break;

                case RequestType.TileWidth:
                    return _tiledWorld.TileProvider.GetTileWidth().Result;
                case RequestType.TileHeight:
                    return _tiledWorld.TileProvider.GetTileHeight().Result;
                case RequestType.Rows:
                    return _tiledWorld.TileProvider.GetRows().Result;
                case RequestType.Columns:
                    return _tiledWorld.TileProvider.GetColumns().Result;
                case RequestType.Path:
                    return _tiledWorld.TileProvider.GetImagePath().Result;
                default:
                    throw new InvalidEnumArgumentException();
            }

            return null;
        }

    }
}
