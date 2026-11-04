using System.Drawing;
using SDL;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL.Software
{
    public unsafe class SdlPmdImageSoft : SdlPmdImage
    {

        private readonly SDL_Texture* _texture;
        private readonly SDL_Renderer* _renderer;

        public SdlPmdImageSoft(PmdComponentConfig config, string imagePath, SDL_Renderer* renderer) : base(config, imagePath)
        {
            _renderer = renderer;

            _texture = SDL_CreateTextureFromSurface(_renderer, LoadBuffer);
            if (_texture == null) throw new ArgumentException($"Unable to create an SDL surface of the image: \n{SDL_GetError()}");

            SDL_SetTextureScaleMode(_texture, SDL_ScaleMode.SDL_SCALEMODE_PIXELART);
            SDL_DestroySurface(LoadBuffer);
        }

        public override void Draw(RectangleF destRect, RectangleF? fromRect, bool centered)
        {
            var displayX = centered ? destRect.X + destRect.Width / 2 : destRect.X;
            var displayY = centered ? destRect.Y + destRect.Height / 2 : destRect.Y;

            SDL_FRect sdlDest;
            sdlDest.w = destRect.Width;
            sdlDest.h = destRect.Height;
            sdlDest.x = displayX;
            sdlDest.y = displayY;

            SDL_FRect sdlFrom;
            if (fromRect == null)
            {
                sdlFrom.w = GetWidth();
                sdlFrom.h = GetHeight();
                sdlFrom.x = 0;
                sdlFrom.y = 0;
            }
            else
            {
                sdlFrom.w = fromRect.Value.Width;
                sdlFrom.h = fromRect.Value.Height;
                sdlFrom.x = fromRect.Value.X;
                sdlFrom.y = fromRect.Value.Y;
            }

            SDL_RenderTexture(_renderer, _texture, &sdlFrom, &sdlDest);
        }

        public override void PostRender()
        {
            SDL_RenderPresent(_renderer);
            base.PostRender();
         
        }

        public override void Cleanup()
        {
            SDL_DestroyTexture(_texture);
        }
    }
}
