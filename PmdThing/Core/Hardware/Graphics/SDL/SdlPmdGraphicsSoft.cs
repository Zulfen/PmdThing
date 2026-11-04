using PmdThing.Core.Hardware.Graphics.SDL.GPU.OpenGL;
using SDL;
using System.Drawing;
using PmdThing.Core.Hardware.Graphics.SDL.Software;
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
            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            SDL_RenderDebugText(_renderer, 0, 0, (Utf8String)$"FPS: {1000 / FrameDelta}");
            SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);

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

        public override void DrawRectangle(RectangleF rect)
        {
            throw new NotImplementedException();
        }

        public override Capabilities GetCapabilities()
        {
            return new Capabilities
            {
                Api = HardwareApi.None,
                ApiVersion = "N/A",
                Passed = true,
                HardwareTransformations = false,
            };
        }
    }
}
