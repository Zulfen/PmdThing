using SDL;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL.GPU
{
    public unsafe class SdlGpuPmdGraphics : PmdGraphics
    {
        private readonly SdlPmdWindow _mainWindow;
        private SDL_Renderer* _renderer;

        private UInt64 _beforeUpdate;
        private float _delta;

        public SdlGpuPmdGraphics(int width, int height, string title) : base(width, height, title)
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
            throw new NotImplementedException();
        }

        public override void DrawRectangle(PmdRect rect)
        {
            throw new NotImplementedException();
        }

    }
}
