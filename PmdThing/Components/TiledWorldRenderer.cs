using GlmSharp;
using PmdThing.Components.Graphics.OpenGL;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Util;
using System.Drawing;
using System.Net.Security;
using PmdThing.Components.Networking;

namespace PmdThing.Components
{
    public class TiledWorldRenderer : PmdContainerComponent<TiledWorld>
    {

        public vec2 Offset { get; set; } = vec2.Zero;
        public vec2 ScreenCenter { get; set; }

        private readonly SpriteSheet _tileSheet;

        public TiledWorldRenderer(PmdComponentConfig config) : base(config)
        {
            DoesRendering = true;
            Priority = 2;

            var window = Framework.Graphics.GetMainWindow();

            ScreenCenter = new vec2(window.GetWidth() / 2f, window.GetHeight() / 2f);

            Register<GlShader>(true, out _,
                @"C:\Users\Joshua Passmore\source\repos\PmdThing\PmdThing\\Core\Hardware\Graphics\Shaders\Vertex\shader.vert",
                @"C:\Users\Joshua Passmore\source\repos\PmdThing\PmdThing\\Core\Hardware\Graphics\Shaders\Fragment\shader.frag",
                "vertex");
            
            Register
            (
                true,
                out _tileSheet,
                ParentContainer.TileProvider.GetImagePath(),
                ParentContainer.TileProvider.GetTileWidth(),
                ParentContainer.TileProvider.GetTileHeight()
            );

            // link all tiles to sprite sheet
            for (var row = 0; row < ParentContainer.TileProvider.GetRows(); row++)
            {
                for (var column = 0; column < ParentContainer.TileProvider.GetColumns(); column++)
                {
                    var tile = ParentContainer.GetTileAt(row, column);
                    tile?.RegisterProperty(ParentContainer.TileProvider.GetSheetLocation(row, column));
                }
            }

        }

        public vec2 GetWorldSpace(PmdTileRef tile)
        {
            return new vec2(
                tile.Column * _tileSheet.GetScaledWidth(),
                tile.Row * _tileSheet.GetScaledHeight()
            );
        }

        public void Camera_OffsetChanged(vec2 currentPos, vec2 prevPos)
        {
            Offset = currentPos;
        }

        public override void Render()
        {

            float screenSpaceY = 0;
            float screenSpaceX = 0;

            for (var row = 0; row < ParentContainer.TileProvider.GetRows(); row++)
            {
                for (var column = 0; column < ParentContainer.TileProvider.GetColumns(); column++)
                {
                    var tileContainer = ParentContainer.GetTileAt(row, column);
                    if (tileContainer == null) continue;

                    var textureRect = tileContainer.GetProperty<RectangleF>();

                    var destRect = new RectangleF
                    {
                        X = screenSpaceX + ScreenCenter.x - Offset.x,
                        Y = screenSpaceY + ScreenCenter.y - Offset.y,
                        Width = _tileSheet.GetSpriteWidth(),
                        Height = _tileSheet.GetSpriteWidth(),
                    };

                    _tileSheet.DrawSprite(textureRect, destRect);

                    screenSpaceX += _tileSheet.GetScaledWidth();
                }

                screenSpaceX = 0;
                screenSpaceY += _tileSheet.GetScaledHeight();

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
