using System.Drawing;
using PmdThing.Core.Hardware.Graphics;

namespace PmdThing.Core.Util
{
    public interface IPmdTileProvider
    {

        string GetImagePath();

        /// <summary>
        /// Get rectangle of tile's texture in the sprite sheet.
        /// </summary>
        /// <param name="row">Row</param>
        /// <param name="column">Column</param>
        /// <returns>Rectangle of location in sprite sheet</returns>
        RectangleF GetSheetLocation(int row, int column);
        bool CollisionCheck(int row, int column);
        int GetTileWidth();
        int GetTileHeight();
        int GetRows();
        int GetColumns();


    }
}
