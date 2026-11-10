using PmdThing.Core;
using PmdThing.Core.Util;
using System.Drawing;

namespace PmdThing.Components.Graphics.UI
{
    /// <summary>
    /// Component value is a rectangle in screen coords.
    /// </summary>
    public class UiBounds : PmdListenableComponent<Rectangle>
    {

        public int TileSize { get; init; }

        public required Point ScreenTopLeft
        {
            get;
            set
            {
                field = value;
                ComponentValue = CalculateScreenBounds(value, TileDimensions, TileSize);
            }
        }

        public required Size TileDimensions
        {
            get;
            set
            {
                field = value;
                ComponentValue = CalculateScreenBounds(ScreenTopLeft, value, TileSize);
            }
        }

        public UiBounds(PmdComponentConfig config, Point screenTopLeft, Size tileDimensions, int tileSize) : base(config, CalculateScreenBounds(screenTopLeft, tileDimensions, tileSize))
        {
            TileDimensions = tileDimensions;
            ScreenTopLeft = screenTopLeft;
            TileSize = tileSize;
        }

        private static Rectangle CalculateScreenBounds(Point screenTopLeft, Size tileDimensions, int tileSize)
        {
            return new Rectangle
            {
                Size = new Size(tileDimensions.Width * tileSize, tileDimensions.Height * tileSize),
                Location = screenTopLeft
            };
        }
    }
}
