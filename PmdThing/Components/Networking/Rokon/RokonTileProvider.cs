using PmdThing.Components.Networking.QueryHandlers;
using PmdThing.Core;
using PmdThing.Core.Util;
using System.Data.Common;
using System.Drawing;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonTileProvider : PmdComponent, IPmdTileProvider
    {

        public RokonTileProvider(PmdComponentConfig config) : base(config)
        {
            if (Client == null) throw new InvalidOperationException("RokonTileProvider can only be used with a client.");
        }

        public async Task<string> GetImagePath()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Path
            };
            var path = await Client!.SendQuery<string>(QueryType.Tile, null, request);

            return path ?? throw new InvalidOperationException("Received null path from Rokon server.");
        }

        public async Task<RectangleF> GetSheetLocation(int row, int column)
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.SheetLocation,
                Tile = new PmdTileRef(row, column)
            };
            return await Client!.SendQuery<RectangleF>(QueryType.Tile, null, request);
        }

        public async Task<bool> CollisionCheck(int row, int column)
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Collision,
                Tile = new PmdTileRef(row, column)
            };
            return await Client!.SendQuery<bool>(QueryType.Tile, null, request);
        }

        public async Task<int> GetTileWidth()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.TileWidth,
            };
            return await Client!.SendQuery<int>(QueryType.Tile, null, request);
        }

        public async Task<int> GetTileHeight()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.TileHeight,
            };
            return await Client!.SendQuery<int>(QueryType.Tile, null, request);
        }

        public async Task<int> GetRows()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Rows,
            };
            return await Client!.SendQuery<int>(QueryType.Tile, null, request);
        }

        public async Task<int> GetColumns()
        {
            var request = new TileQuery.TileRequest
            {
                Type = TileQuery.RequestType.Columns,
            };
            return await Client!.SendQuery<int>(QueryType.Tile, null, request);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
