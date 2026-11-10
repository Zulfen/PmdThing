using GlmSharp;
using OpenTK.Graphics.OpenGL;
using PmdThing.Components.Graphics.OpenGL;
using SDL;
using System.Drawing;
using static SDL.SDL3;


namespace PmdThing.Core.Hardware.Graphics.SDL.GPU.OpenGL
{
    public unsafe class SdlPmdImageGl : SdlPmdImage
    {

        private readonly GlShader _shader;
        private readonly SdlPmdTextureAtlasGl _atlas;
        private readonly PointF _atlasTopLeft;

        // TODO: indices
        // https://www.opengl-tutorial.org/beginners-tutorials/tutorial-5-a-textured-cube/
        // uv's have the origin in the bottom left
        private readonly float[] _packedSquare = [ 
            0.0f, 1.0f, 0.0f, 0.0f,
            1.0f, 0.0f, 1.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 1.0f,

            0.0f, 1.0f, 0.0f, 0.0f,
            1.0f, 1.0f, 1.0f, 0.0f,
            1.0f, 0.0f, 1.0f, 1.0f
        ];

        public SdlPmdImageGl(PmdComponentConfig config, string path) : base(config, path)
        {
            _atlas = GetSharedComponent<SdlPmdTextureAtlasGl>();
            _shader = GetSharedComponent<GlShader>();

            if (_shader.IsBufferEmpty())
            {
                _shader.AddToBuffer(_packedSquare);
            }

            var converted = SDL_ConvertSurface(LoadBuffer, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
            SDL_DestroySurface(LoadBuffer);
            LoadBuffer = converted;

            // Where this image is in the big shared atlas
            _atlasTopLeft = _atlas.AddTexture(this);
            _atlas.Stitch();
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override void Draw(RectangleF destRect, RectangleF? fromRect, bool centered)
        {

            // This is relative to the actual image that is inside the big GL atlas
            RectangleF subAtlasRect;
            if (fromRect == null)
            {
                // if you want to draw the whole texture atlas for some reason?...
                subAtlasRect = new Rectangle()
                {
                    X = 0,
                    Y = 0,
                    Width = GetWidth(),
                    Height = GetHeight()
                };
            }
            else
            {
                subAtlasRect = Rectangle.Round(fromRect.Value);
            }

            if (centered)
            {
                destRect.X += (float)GetWidth() / 2;
                destRect.Y += (float)GetHeight() / 2;
            }

            var windowWidth = Framework.Graphics
                .GetMainWindow()
                .GetWidth();

            var windowHeight = Framework.Graphics
                .GetMainWindow()
                .GetHeight();

            // TODO: maybe cache these and only update on window resize
            var projectionMatrix = mat4.Ortho(0, windowWidth, windowHeight, 0, -1, 1);
            // the transformations applied to the base square
            var modelMatrix = mat4.Translate(destRect.X, destRect.Y, 0) * mat4.Scale(destRect.Width, destRect.Height, 1);

            var atlasLocation = new vec4
            (
                (_atlasTopLeft.X + subAtlasRect.X) / _atlas.GetAtlasWidth(this),
                1f - (_atlasTopLeft.Y + subAtlasRect.Y + subAtlasRect.Height) / _atlas.GetAtlasHeight(this), 
                subAtlasRect.Width / _atlas.GetAtlasWidth(this),
                subAtlasRect.Height / _atlas.GetAtlasHeight(this)
            );

            _shader.Use();

            _shader.SetMat4("model", modelMatrix);
            _shader.SetMat4("projection", projectionMatrix);
            _shader.SetVec3("spriteColor", new vec3(1, 1, 1));
            _shader.SetVec4("atlasRect", atlasLocation);

            _atlas.Use(this);

            // draw
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // TODO: batch, lots of draw calls going on
        }

    }
}
