using GlmSharp;
using PmdThing.Components.Graphics.OpenGL;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;
using System.Drawing;

namespace PmdThing.Components
{
    public class TiledWorldRenderer : PmdContainerComponent<TiledWorld>
    {

        public vec2 Offset { get; set; } = vec2.Zero;
        public vec2 ScreenCenter { get; set; }

        public int Rows { get; private set; }
        public int Columns { get; private set; }

        private SpriteSheet? _tileSheet;

        public TiledWorldRenderer(PmdComponentConfig config) : base(config)
        {
            DoesRendering = true;

            Priority = 2;

            var window = Framework.Graphics.GetMainWindow();

            ScreenCenter = new vec2(window.GetWidth() / 2f, window.GetHeight() / 2f);

            Register<GlShader>(true, out _,
                @"Core\Hardware\Graphics\Shaders\Vertex\shader.vert",
                @"Core\Hardware\Graphics\Shaders\Fragment\shader.frag",
                "vertex");
        }

        public async Task SetData(int rows, int columns, string imagePath)
        {
            Rows = rows;
            Columns = columns;

            SubmitTask(() =>
            {
                _tileSheet?.Cleanup();
                Register
                (
                    true,
                    out SpriteSheet registeredSheet,
                    imagePath,
                    Rows,
                    Columns
                );
                _tileSheet = registeredSheet;

            }, ProcessingOrder.Update);
            
            // link all tiles to sprite sheet
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var tile = ParentContainer.GetTileAt(row, column);
                    var imageRect = await ParentContainer.TileProvider.GetSheetLocation(row, column);
                    tile?.RegisterProperty(imageRect);
                }
            }
        }

        public void Camera_OffsetChanged(vec2 currentPos, vec2 prevPos)
        {
            Offset = currentPos;
        }

        public override void Render()
        {
            if (_tileSheet == null) return;

            float screenSpaceY = 0;
            float screenSpaceX = 0;

            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var tileContainer = ParentContainer.GetTileAt(row, column);
                    if (tileContainer == null) continue;

                    if (!tileContainer.HasProperty<RectangleF>()) continue;
                    var textureRect = tileContainer.GetProperty<RectangleF>();

                    var destRect = new RectangleF
                    {
                        X = screenSpaceX + ScreenCenter.x - Offset.x,
                        Y = screenSpaceY + ScreenCenter.y - Offset.y,
                        Width = _tileSheet?.GetSpriteWidth() ?? 0,
                        Height = _tileSheet?.GetSpriteWidth() ?? 0,
                    };

                    _tileSheet?.DrawSprite(textureRect, destRect);

                    screenSpaceX += _tileSheet?.GetScaledWidth() ?? 0;
                }

                screenSpaceX = 0;
                screenSpaceY += _tileSheet?.GetScaledHeight() ?? 0;

            }

        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override Type? TickBefore()
        {
            return typeof(SpriteRenderer);
        }
    }
}
