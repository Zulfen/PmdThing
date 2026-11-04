using SDL;
using System.Drawing;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL
{
   
    public abstract class SdlPmdGraphicsBase : PmdGraphics
    {

        protected readonly SdlPmdWindow MainWindow;

        private UInt64 _beforeUpdate;
        protected float FrameDelta;

        protected SdlPmdGraphicsBase(int width, int height, string title, SDL_WindowFlags flags = 0) : base(width, height, title)
        {
            SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO);
            MainWindow = new SdlPmdWindow(width, height, title, flags | SDL_WindowFlags.SDL_WINDOW_HIDDEN);
        }

        public override void Cleanup()
        {
            MainWindow.Destroy();
        }

        public override void PreUpdate()
        {
            _beforeUpdate = SDL_GetPerformanceCounter();
        }

        public override void PostRender()
        {
            var afterUpdate = SDL_GetPerformanceCounter();
            FrameDelta = (float)((afterUpdate - _beforeUpdate) * 1000.0 / SDL_GetPerformanceFrequency());
        }

        public override PmdImage LoadImage(PmdComponent parent, string path)
        {
            return CreateSdlImage(parent, path);
        }

        protected abstract SdlPmdImage CreateSdlImage(PmdComponent parent, string path);

        public override PmdWindow CreateWindow(int width, int height)
        {
            throw new NotImplementedException();
        }

        public override float GetDelta()
        {
            return FrameDelta;
        }

        public override PmdWindow GetMainWindow()
        {
            return MainWindow;
        }

    }
}
