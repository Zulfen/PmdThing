using PmdThing.Core.Hardware.Graphics.SDL;
using PmdThing.Core.Hardware.Input.SDL;
using SDL;
using static SDL.SDL3;
using static SDL.SDL_EventType;

namespace PmdThing.Core.Hardware.Framework.SDL
{
    public unsafe class SdlPmdFramework(SdlPmdGraphicsBase sdlGraphics, SdlPmdKeyboard keyboard) : PmdFramework(sdlGraphics, keyboard)
    {

        public override void Init()
        {
            Graphics
                .GetMainWindow()
                .Show();
        }

        public override void Shutdown()
        {
            Graphics.Cleanup();
        }

        public override void Update()
        {
            SDL_Event sdlEvent;
            if (!SDL_PollEvent(&sdlEvent)) return;

            switch (sdlEvent.Type)
            {
                case SDL_EVENT_QUIT:
                    Running = false;
                    break;
            }

            // maybe check if it's key up or down here then handle it
            keyboard.Poll(&sdlEvent);

        }

        public override string ToString()
        {
            return "SDL";
        }
    }
}
