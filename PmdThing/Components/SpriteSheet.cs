using System.Net.Sockets;
using System.Xml.Linq;
using System.Xml.Serialization;
using PmdThing.Core;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Util;

namespace PmdThing.Components
{
    public class SpriteSheet : PmdComponent
    {

        private readonly List<PmdRect[]> _animationRow = [];
        private readonly PmdImage _spriteSheetImage;
        private readonly PmdConstants.AnimationType _type;

        private readonly int _spriteWidth;
        private readonly int _spriteHeight;

        public const int SpriteScale = 2;

        private readonly int _columns;
        private readonly int _rows;

        // https://moonside.games/posts/sdl-gpu-sprite-batcher/

        public SpriteSheet(PmdComponentConfig config, string path, int spriteWidth, int spriteHeight) : base(config)
        {

            if (!Framework.Graphics.LoadImage(path, out _spriteSheetImage)) return;

            _spriteHeight = spriteHeight;
            _spriteWidth = spriteWidth;

            _rows = _spriteSheetImage.GetHeight() / spriteHeight;
            _columns = _spriteSheetImage.GetWidth() / spriteWidth;

            var widthCounter = 0;
            var heightCounter = 0;
            for (var row = 0; row < _rows; row++)
            {

                List<PmdRect> rects = [];

                for (var column = 0; column < _columns; column++)
                {
                    PmdRect newRect = new()
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

        public int GetScaledSpriteGetSize()
        {
            return _spriteWidth * SpriteScale;
        }

        public int GetRows()
        {
            return _rows;
        }

        public int GetColumns()
        {
            return _columns;
        }

        public void DrawSprite(int x, int y, int row, int column, bool centered = true)
        {
            if (row > _rows || column > _columns) throw new IndexOutOfRangeException(
                $"Tried to draw row {row},{column}, which exceeds the max amount of {_rows},{_columns}");

            var rowRects = _animationRow[row];
            var sourceRect = rowRects[column];

            var destRect = new PmdRect
            {
                X = x,
                Y = y,
                Width = _spriteWidth * SpriteScale,
                Height = _spriteHeight * SpriteScale,
            };

            _spriteSheetImage.Draw(destRect, sourceRect, centered);
        }

    }
}
