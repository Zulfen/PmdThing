using PmdThing.Core.Hardware.Graphics.SDL.Software;
using SDL;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL
{
    /// <summary>
    /// Base, unaccelerated SDL renderer which can be used as a fallback if the hardware doesn't support
    /// the graphics API you are targeting
    /// </summary>
    public unsafe class SdlPmdGraphicsSoft : SdlPmdGraphicsBase
    {

        private SDL_Renderer* _renderer;

        public SdlPmdGraphicsSoft(int width, int height, string title, SDL_WindowFlags flags = 0) : base(width, height, title)
        {
            _renderer = SDL_CreateRenderer(MainWindow.GetHandle(), (Utf8String)null);
        }

        public override void Cleanup()
        {
            MainWindow.Destroy();
            _renderer = null;
        }

        public override void PreRender()
        {
            SDL_RenderClear(_renderer);
        }

        public override void PostRender()
        {
            SDL_RenderPresent(_renderer);
            base.PostRender();
        }

        public override PmdImage LoadImage(PmdComponent parent, string path)
        {
            return CreateImage(parent, path);
        }

        protected override SdlPmdImage CreateImage(PmdComponent parent, string path)
        {
            parent.Register<SdlPmdImageSoft>(true, out var image, path, *_renderer);
            return image;
        }

        public override PmdWindow CreateWindow(int width, int height)
        {
            throw new NotImplementedException();
        }

        public override Capabilities GetCapabilities()
        {
            return new Capabilities
            {
                Headless = false,
                Api = HardwareApi.None,
                ApiVersion = "N/A",
                Passed = true,
                HardwareTransformations = false,
            };
        }
    }
}
