using SDL;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace PmdThing.Core.Hardware.Graphics.SDL
{
    public unsafe class SdlPmdImage(string path, SDL_Renderer* renderer) : PmdImage
    {
        private SDL_Texture* _texture;
        private bool _destroyed;

        private int _width;
        private int _height;

        public override void Draw(PmdRect destRect, PmdRect? fromRect, bool centered)
        {

            var displayX = centered ? destRect.X + (float) destRect.Width / 2 : destRect.X;
            var displayY = centered ? destRect.Y + (float) destRect.Height / 2 : destRect.Y;

            SDL_FRect sdlDest;
            sdlDest.w = destRect.Width;
            sdlDest.h = destRect.Height;
            sdlDest.x = displayX;
            sdlDest.y = displayY;

            SDL_FRect sdlFrom;
            if (fromRect == null)
            {
                sdlFrom.w = _width;
                sdlFrom.h = _height;
                sdlFrom.x = 0;
                sdlFrom.y = 0;
            }
            else
            {
                sdlFrom.w = fromRect.Width;
                sdlFrom.h = fromRect.Height;
                sdlFrom.x = fromRect.X;
                sdlFrom.y = fromRect.Y;
            }

            SDL_RenderTexture(renderer, _texture, &sdlFrom, &sdlDest);

        }

        public override int GetHeight()
        {
            return _height;
        }

        public override int GetWidth()
        {
            return _width;
        }

        public override bool Load()
        {

            // if already exists (hello sdl3 tutorials :3)
            var loadedSurface = IMG_Load((Utf8String)path);
            if (loadedSurface == null) throw new ArgumentException($"Unable to IMG_Load the image at: {path}\n{SDL_GetError()}");

            _texture = SDL_CreateTextureFromSurface(renderer, loadedSurface);
            if (_texture == null) throw new ArgumentException($"Unable to create an SDL surface of the image at: {path}\n{SDL_GetError()}");
            _width = loadedSurface->w;
            _height = loadedSurface->h;

            SDL_SetTextureScaleMode(_texture, SDL_ScaleMode.SDL_SCALEMODE_PIXELART);
            SDL_DestroySurface(loadedSurface);

            return true;

        }

        public override void Resize(int width, int height)
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            SDL_DestroyTexture(_texture);
            _texture = null;
            _destroyed = true;
        }

        ~SdlPmdImage()
        {
            if (!_destroyed) Destroy();
        }

    }
}
