using System.Drawing;
using Microsoft.Extensions.Logging;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace PmdThing.Core.Hardware.Graphics.SDL
{
    public abstract unsafe class SdlPmdImage : PmdImage
    {
        public required SDL_Surface* LoadBuffer { get; init; }
        private readonly int _width;
        private readonly int _height;

        private bool _destroyed;

        protected SdlPmdImage(PmdComponentConfig config, string path) : base(config)
        {
            Framework.Logger.LogDebug("Loading image at path: ({path})..", path);
            LoadBuffer = IMG_Load((Utf8String)path);

            _width = LoadBuffer->w;
            _height = LoadBuffer->h;

            if (LoadBuffer == null)
            {
                throw new ArgumentException($"Unable to IMG_Load the image at: {path}\n{SDL_GetError()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destRect"></param>
        /// <param name="fromRect"></param>
        /// <param name="centered"></param>
        public abstract override void Draw(RectangleF destRect, RectangleF? fromRect, bool centered);

        public override int GetHeight()
        {
            return _height;
        }

        public override int GetWidth()
        {
            return _width;
        }

        public override void Resize(int width, int height)
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            _destroyed = true;
        }

        ~SdlPmdImage()
        {
            if (!_destroyed) Destroy();
        }

    }
}
