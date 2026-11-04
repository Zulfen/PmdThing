using System.Drawing;
using PmdThing.Core;
using PmdThing.Core.Hardware.Graphics;

namespace PmdThing.Components
{
    public class SpriteSheet : PmdComponent
    {

        private readonly List<RectangleF[]> _animationRow = [];
        private readonly PmdImage _spriteSheetImage;

        private readonly int _spriteWidth;
        private readonly int _spriteHeight;

        public const int SpriteScale = 2;

        private readonly int _columns;
        private readonly int _rows;

        // add location

        // https://moonside.games/posts/sdl-gpu-sprite-batcher/

        public SpriteSheet(PmdComponentConfig config, string path, int spriteWidth, int spriteHeight) : base(config)
        {

            _spriteSheetImage = Framework.Graphics.LoadImage(this, path);

            _spriteHeight = spriteHeight;
            _spriteWidth = spriteWidth;

            _rows = _spriteSheetImage.GetHeight() / spriteHeight;
            _columns = _spriteSheetImage.GetWidth() / spriteWidth;

            var widthCounter = 0;
            var heightCounter = 0;
            for (var row = 0; row < _rows; row++)
            {

                List<RectangleF> rects = [];

                for (var column = 0; column < _columns; column++)
                {
                    RectangleF newRect = new()
                    {
                        Width = spriteWidth,
                        Height = spriteHeight,
                        X = widthCounter,
                        Y = heightCounter
                    };
                    rects.Add(newRect);
                    widthCounter += spriteWidth;
                }

                widthCounter = 0;
                heightCounter += spriteHeight;

                _animationRow.Add([.. rects]);

            }
        }

        public override void Cleanup()
        {
            _spriteSheetImage.Destroy();
        }

        public int GetScaledWidth()
        {
            return _spriteWidth * SpriteScale;
        }

        public int GetScaledHeight()
        {
            return _spriteHeight * SpriteScale;
        }

        public int GetSpriteWidth()
        {
            return _spriteWidth;
        }

        public int GetSpriteHeight()
        {
            return _spriteHeight;
        }

        public int GetRows()
        {
            return _rows;
        }

        public int GetColumns()
        {
            return _columns;
        }

        public PmdImage GetImage()
        {
            return _spriteSheetImage;
        }

        // make big texture atlas for everything
        // store ogl float coords in here. probably calculate them here
        // List<Atlas> with ogl start and end coords?
        public void DrawSprite(float x, float y, int row, int column, bool centered = true)
        {
            if (row > _rows || column > _columns) throw new IndexOutOfRangeException(
                $"Tried to draw row {row},{column}, which exceeds the max amount of {_rows},{_columns}");

            var rowRects = _animationRow[row];
            var sourceRect = rowRects[column];

            var destRect = new RectangleF
            {
                X = x,
                Y = y,
                Width = _spriteWidth * SpriteScale,
                Height = _spriteHeight * SpriteScale,
            };

            _spriteSheetImage.Draw(destRect, sourceRect, centered);

        }

        public void DrawSprite(RectangleF sourceRect, RectangleF destRect, bool centered = false)
        {
            destRect.Width *= SpriteScale;
            destRect.Height *= SpriteScale;

            if (centered)
            {
                destRect.X += (float)GetSpriteWidth() / 2;
                destRect.Y += (float)GetSpriteHeight() / 2;
            }

            _spriteSheetImage.Draw(destRect, sourceRect, false);
        }

    }
}
