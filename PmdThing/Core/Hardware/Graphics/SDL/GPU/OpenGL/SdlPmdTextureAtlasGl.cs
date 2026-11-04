using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;
using SDL;
using System.Drawing;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL.GPU.OpenGL
{
    public unsafe class SdlPmdTextureAtlasGl : PmdTextureAtlas<SdlPmdImage>
    {
        private const int MaxWidth = 1024;
        private const int MaxHeight = 1024;

        public class AtlasEntry
        {
            public Point LatestPoint;
            public SdlPmdImage? LatestImage;

            public List<int> RowHeights = [];

            public Dictionary<SdlPmdImage, Point> TextureLocations = [];
            public int TextureId = GL.GenTexture();
            public SDL_Surface* Surface = SDL_CreateSurface(MaxWidth, MaxHeight, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
        }

        private readonly Dictionary<SdlPmdImage, AtlasEntry> _imageAtlas = [];
        private AtlasEntry _currentAtlas = new();

        public SdlPmdTextureAtlasGl(PmdComponentConfig config) : base(config)
        {
            GL.GetInteger(GetPName.MaxTextureImageUnits, out var maxUnits);
            Framework.Logger.LogDebug("Max OpenGL texture units supported: {units}", maxUnits);
        }

        public override Point AddTexture(SdlPmdImage image)
        {
            if (image.LoadBuffer == null) throw new ArgumentException("SDL surface is null");

            Point finalPosition = new();

            var addFailed = true;
            while (addFailed)
            {
                var penX = _currentAtlas.LatestPoint.X;
                if (_currentAtlas.LatestImage != null)
                {
                    var width = _currentAtlas.LatestImage.GetWidth();
                    penX += width;
                }

                _currentAtlas.RowHeights.Add(image.GetHeight());

                var nextX = penX + image.GetWidth();
                var nextY = _currentAtlas.LatestPoint.Y + _currentAtlas.RowHeights.Max() + image.GetHeight();

                if (nextX >= MaxWidth)
                {
                    if (_currentAtlas.LatestPoint is { Y: 0, X: 0 })
                    {
                        finalPosition.Y = 0;
                        finalPosition.X = 0;
                    }
                    else
                    {
                        finalPosition.X = 0;
                        finalPosition.Y = _currentAtlas.LatestPoint.Y + _currentAtlas.RowHeights.Max();
                        
                    }

                    _currentAtlas.RowHeights.Clear();
                }
                else 
                {
                    finalPosition.X = penX;
                    finalPosition.Y = _currentAtlas.LatestPoint.Y;
                }

                if (nextY > MaxHeight && _currentAtlas.LatestPoint.Y > 0)
                {
                    // whatever
                    _currentAtlas = new AtlasEntry
                    {
                        LatestImage = image,
                    };
                }
                else
                {
                    addFailed = false;
                }

            }

            SDL_Rect destRect;
            destRect.x = finalPosition.X;
            destRect.y = finalPosition.Y;
            SDL_BlitSurface(image.LoadBuffer, null, _currentAtlas.Surface, &destRect);
            // hmm. don't see any reason to use the pixels after. might change my mind
            SDL_DestroySurface(image.LoadBuffer);


            AddAtlasEntry(image, finalPosition);
            _imageAtlas.Add(image, _currentAtlas);

            return finalPosition;
        }

        private void AddAtlasEntry(SdlPmdImage image, Point topLeft)
        {
            _currentAtlas.TextureLocations.Add(image, topLeft);
            _currentAtlas.LatestImage = image;
            _currentAtlas.LatestPoint = topLeft;
        }

        public override void Stitch()
        {
            var tempAtlas = SDL_CreateSurface
            (
                _currentAtlas.Surface->w,
                _currentAtlas.Surface->h,
                SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888
            );

            SDL_BlitSurface(_currentAtlas.Surface, null, tempAtlas, null);
            SDL_FlipSurface(tempAtlas, SDL_FlipMode.SDL_FLIP_VERTICAL);

            SDL_SaveBMP(tempAtlas, $"atlas{_currentAtlas.TextureId}.bmp");

            GL.BindTexture(TextureTarget.Texture2D, _currentAtlas.TextureId);

            SDL_LockSurface(tempAtlas);

            GL.TexImage2D
            (
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                tempAtlas->w,
                tempAtlas->h,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                tempAtlas->pixels
            );

            SDL_UnlockSurface(tempAtlas);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            SDL_DestroySurface(tempAtlas);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override void RemoveTexture(SdlPmdImage image)
        {
            // TODO
            throw new NotImplementedException();
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override void Use(SdlPmdImage image)
        {
            var underlyingAtlas = _imageAtlas[image].TextureId;
            GL.BindTexture(TextureTarget.Texture2D, underlyingAtlas);
        }

        public override int GetAtlasWidth(SdlPmdImage image)
        {
            var underlyingAtlas = _imageAtlas[image].Surface;
            return underlyingAtlas->w;
        }

        public override int GetAtlasHeight(SdlPmdImage image)
        {
            var underlyingAtlas = _imageAtlas[image].Surface;
            return underlyingAtlas->h;
        }

        private static void CheckError()
        {
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                throw new Exception("GL error:" + error);
            }
        }

    }
}
