using PmdThing.Components.Networking.QueryHandlers;
using PmdThing.Core;
using PmdThing.Core.Util;
using System.Data.Common;
using System.Drawing;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonTileProvider : PmdComponent, IPmdTileProvider
    {

        private readonly RokonClientConnection _client;

        public RokonTileProvider(PmdComponentConfig config) : base(config)
        {
            _client = GetParentComponent<RokonClientConnection>();
        }

        public async Task<string> GetImagePath()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Path
            };
            return await _client.SendQuery<string>(QueryType.Tile, request);
        }

        public async Task<RectangleF> GetSheetLocation(int row, int column)
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.SheetLocation,
                Tile = new PmdTileRef(row, column)
            };
            return await _client.SendQuery<RectangleF>(QueryType.Tile, request);
        }

        public async Task<bool> CollisionCheck(int row, int column)
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Collision,
                Tile = new PmdTileRef(row, column)
            };
            return await _client.SendQuery<bool>(QueryType.Tile, request);
        }

        public async Task<int> GetTileWidth()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.TileWidth,
            };
            return await _client.SendQuery<int>(QueryType.Tile, request);
        }

        public async Task<int> GetTileHeight()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.TileHeight,
            };
            return await _client.SendQuery<int>(QueryType.Tile, request);
        }

        public async Task<int> GetRows()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Rows,
            };
            return await _client.SendQuery<int>(QueryType.Tile, request);
        }

        public async Task<int> GetColumns()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Columns,
            };
            return await _client.SendQuery<int>(QueryType.Tile, request);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
