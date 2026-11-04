using SDL;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL
{
    public unsafe class SdlPmdWindow : PmdWindow
    {
        public event EventHandler? WindowClosed;
        private readonly SDL_Window* _window;
        private readonly SDL_Renderer* _renderer;

        public SdlPmdWindow(int width, int height, string title) : base(width, height, title)
        {
            fixed (SDL_Window** windowPtr = &_window)
            {
                fixed (SDL_Renderer** rendererPtr = &_renderer)
                {
                    if (!SDL_CreateWindowAndRenderer((Utf8String)Title, Width, Height, SDL_WindowFlags.SDL_WINDOW_HIDDEN, windowPtr, rendererPtr))
                    {
                        throw new Exception($"Failed to create SDL window / renderer: {SDL_GetError()}");
                    }
                }
            }
        }

        public override void Destroy()
        {
            SDL_DestroyWindow(_window);
            SDL_DestroyRenderer(_renderer);
        }

        public override void SetFullscreen(bool fullscreen)
        {
            SDL_SetWindowFullscreen(_window, fullscreen);
        }

        public SDL_Window* GetHandle()
        {
            return _window;
        }

        public SDL_Renderer* GetSdlRenderer()
        {
            return _renderer;
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
