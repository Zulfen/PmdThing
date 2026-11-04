using System.Drawing;

namespace PmdThing.Core.Util
{
    public interface IPmdTileProvider
    {

        Task<string> GetImagePath();

        /// <summary>
        /// Get rectangle of tile's texture in the sprite sheet.
        /// </summary>
        /// <param name="row">Row</param>
        /// <param name="column">Column</param>
        /// <returns>Rectangle of location in sprite sheet</returns>
        Task<RectangleF> GetSheetLocation(int row, int column);
        Task<bool> CollisionCheck(int row, int column);
        Task<int> GetTileWidth();
        Task<int> GetTileHeight();
        Task<int> GetRows();
        Task<int> GetColumns();


    }
}
