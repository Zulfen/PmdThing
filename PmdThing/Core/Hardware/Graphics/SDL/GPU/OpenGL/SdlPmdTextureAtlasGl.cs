using OpenTK.Graphics.OpenGL;
using SDL;
using System.Drawing;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL.GPU.OpenGL
{
    public unsafe class SdlPmdTextureAtlasGl(PmdComponentConfig config) : PmdTextureAtlas<SdlPmdImage>(config)
    {
        private const int MaxWidth = 2048;
        private const int MaxHeight = 2048;

        public class SubAtlasEntry
        {
            public Point LatestPoint;
            public SdlPmdImage? LatestImage;

            public List<int> RowHeights = [];

            public Dictionary<SdlPmdImage, Point> TextureLocations = [];
            public int TextureId = GL.GenTexture();
            public SDL_Surface* Surface = SDL_CreateSurface(MaxWidth, MaxHeight, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
        }

        private readonly Dictionary<SdlPmdImage, SubAtlasEntry> _imageAtlasTracker = [];
        private SubAtlasEntry _currentSubAtlas = new();

        public override Point AddTexture(SdlPmdImage image)
        {
            if (image.LoadBuffer == null) throw new ArgumentException("SDL surface is null");

            Point finalPosition = new();

            // Below is a sort of dynamic (ladder?) stitch I wrote by hand.
            // It seems to work fine when there's a handful of sprites, but I have seen it blow up when there's tons.
            // 2048x2048 seems the best size for the PMD sprites atm
            // TODO: revisit this

            var addFailed = true;
            while (addFailed)
            {
                var penX = _currentSubAtlas.LatestPoint.X;
                if (_currentSubAtlas.LatestImage != null)
                {
                    var width = _currentSubAtlas.LatestImage.GetWidth();
                    penX += width;
                }

                _currentSubAtlas.RowHeights.Add(image.GetHeight());

                var nextX = penX + image.GetWidth();
                var nextY = _currentSubAtlas.LatestPoint.Y + _currentSubAtlas.RowHeights.Max() + image.GetHeight();

                if (nextX >= MaxWidth)
                {
                    if (_currentSubAtlas.LatestPoint is { Y: 0, X: 0 })
                    {
                        finalPosition.Y = 0;
                        finalPosition.X = 0;
                    }
                    else
                    {
                        finalPosition.X = 0;
                        finalPosition.Y = _currentSubAtlas.LatestPoint.Y + _currentSubAtlas.RowHeights.Max();
                        
                    }

                    _currentSubAtlas.RowHeights.Clear();
                }
                else 
                {
                    finalPosition.X = penX;
                    finalPosition.Y = _currentSubAtlas.LatestPoint.Y;
                }

                if (nextY > MaxHeight && _currentSubAtlas.LatestPoint.Y > 0)
                {
                    _currentSubAtlas = new SubAtlasEntry();
                }
                else
                {
                    addFailed = false;
                }

            }

            SDL_Rect destRect;
            destRect.x = finalPosition.X;
            destRect.y = finalPosition.Y;
            SDL_BlitSurface(image.LoadBuffer, null, _currentSubAtlas.Surface, &destRect);
            // hmm. don't see any reason to use the pixels after. might change my mind
            SDL_DestroySurface(image.LoadBuffer);

            AddToCurrentAtlas(image, finalPosition);
            _imageAtlasTracker.Add(image, _currentSubAtlas);

            return finalPosition;
        }

        private void AddToCurrentAtlas(SdlPmdImage image, Point topLeft)
        {
            _currentSubAtlas.TextureLocations.Add(image, topLeft);
            _currentSubAtlas.LatestImage = image;
            _currentSubAtlas.LatestPoint = topLeft;
        }

        /// <summary>
        /// Adds the current sub-atlas to the large shared atlas.
        /// </summary>
        public override void Stitch()
        {
            var tempAtlas = SDL_CreateSurface
            (
                _currentSubAtlas.Surface->w,
                _currentSubAtlas.Surface->h,
                SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888
            );

            SDL_BlitSurface(_currentSubAtlas.Surface, null, tempAtlas, null);
            SDL_FlipSurface(tempAtlas, SDL_FlipMode.SDL_FLIP_VERTICAL);
            
            // debug
            SDL_SaveBMP(tempAtlas, $"atlas{_currentSubAtlas.TextureId}.bmp");

            GL.BindTexture(TextureTarget.Texture2D, _currentSubAtlas.TextureId);

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
            // TODO: lol
            throw new NotImplementedException();
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override void Use(SdlPmdImage image)
        {
            var underlyingAtlas = _imageAtlasTracker[image].TextureId;
            GL.BindTexture(TextureTarget.Texture2D, underlyingAtlas);
        }

        public override int GetAtlasWidth(SdlPmdImage image)
        {
            var underlyingAtlas = _imageAtlasTracker[image].Surface;
            return underlyingAtlas->w;
        }

        public override int GetAtlasHeight(SdlPmdImage image)
        {
            var underlyingAtlas = _imageAtlasTracker[image].Surface;
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
