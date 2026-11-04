using PmdThing.Core.Hardware.Framework.SDL;
using SDL;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL
{
    public unsafe class SdlPmdGraphics : PmdGraphics
    {

        private readonly SdlPmdWindow _mainWindow;
        private SDL_Renderer* _renderer;

        private UInt64 _beforeUpdate;
        private float _delta;

        public SdlPmdGraphics(int width, int height, string title) : base(width, height, title)
        {
            _mainWindow = new SdlPmdWindow(width, height, title);
            _renderer = _mainWindow.GetSdlRenderer();
        }

        public override void Cleanup()
        {
            _mainWindow.Destroy();
            _renderer = null;
        }

        public override void PreUpdate()
        {
            _beforeUpdate = SDL_GetPerformanceCounter();
        }

        public override void PreRender()
        {
            SDL_RenderClear(_renderer);
        }
        
        public override void PostRender()
        {

            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            SDL_RenderDebugText(_renderer, 0, 0, (Utf8String)$"FPS: {1000 / _delta}");
            SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);

            SDL_RenderPresent(_renderer);
            var afterUpdate = SDL_GetPerformanceCounter();
            _delta = (float)((afterUpdate - _beforeUpdate) * 1000.0 / SDL_GetPerformanceFrequency());

        }

        public override PmdWindow CreateWindow(int width, int height)
        {
            throw new NotImplementedException();
        }

        public override float GetDelta()
        {
            return _delta;
        }

        public override PmdWindow GetMainWindow()
        {
            return _mainWindow;
        }

        public override bool LoadImage(string path, out PmdImage image)
        {
            var sdlImage = new SdlPmdImage(path, _renderer);
            if (sdlImage.Load())
            {
                image = sdlImage;
                return true;
            }
            
            throw new ArgumentException($"Could not load image from path {path}");
            
        }

        public override void DrawLine(float x1, float y1, float x2, float y2)
        {
            SDL_SetRenderDrawColor(_renderer, 255, 0, 0, 255);
            SDL_RenderLine(_renderer, x1, y1, x2, y2);
        }

        public override void DrawRectangle(PmdRect rect)
        {
            throw new NotImplementedException();
        }
    }
}
