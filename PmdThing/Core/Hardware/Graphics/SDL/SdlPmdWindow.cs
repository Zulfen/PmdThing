using SDL;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL
{
    public unsafe class SdlPmdWindow : PmdWindow
    {
        public event EventHandler? WindowClosed;
        private readonly SDL_Window* _window;

        public SdlPmdWindow(int width, int height, string title, SDL_WindowFlags flags) : base(width, height, title)
        {
            _window = SDL_CreateWindow((Utf8String)Title, Width, Height, flags);
        }

        public override void Destroy()
        {
            SDL_DestroyWindow(_window);
        }

        public override void SetFullscreen(bool fullscreen)
        {
            SDL_SetWindowFullscreen(_window, fullscreen);
        }

        public SDL_Window* GetHandle()
        {
            return _window;
        }

        protected override void SetSizeImpl()
        {
            SDL_SetWindowSize(_window, Width, Height);
        }

        public override void Show()
        {
            SDL_ShowWindow(_window);
        }

        public override void Hide()
        {
            SDL_HideWindow(_window);
        }

        ~SdlPmdWindow()
        {
            Destroy();
        }


    }
}
