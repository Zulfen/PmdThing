using SDL;
using static SDL.SDL3;
using static SDL.SDL_EventType;

namespace PmdThing.Core.Hardware.Input.SDL
{
    public unsafe class SdlPmdKeyboard : IPmdKeyboard
    {

        public event IPmdKeyboard.OnKey? KeyDown;
        public event IPmdKeyboard.OnKey? KeyUp;

        private readonly Dictionary<SDL_Keycode, PmdKey> _pmdKeyMap = new()
        {
            { SDL_Keycode.SDLK_UP, PmdKey.UpArrow },
            { SDL_Keycode.SDLK_DOWN, PmdKey.DownArrow },
            { SDL_Keycode.SDLK_LEFT, PmdKey.LeftArrow },
            { SDL_Keycode.SDLK_RIGHT, PmdKey.RightArrow },
        };

        private readonly Dictionary<PmdKey, SDL_Scancode> _pmdScancodeMap = new()
        {
            { PmdKey.UpArrow, SDL_Scancode.SDL_SCANCODE_UP },
            { PmdKey.DownArrow, SDL_Scancode.SDL_SCANCODE_DOWN },
            { PmdKey.LeftArrow, SDL_Scancode.SDL_SCANCODE_LEFT },
            { PmdKey.RightArrow, SDL_Scancode.SDL_SCANCODE_RIGHT },
        };

        public bool IsKeyDown(PmdKey key)
        {
            var keyStates = SDL_GetKeyboardState(null);
            if (_pmdScancodeMap.TryGetValue(key, out var sdlKey))
            {
                return keyStates != null && keyStates[(int)sdlKey];
            }
            return false;
        }

        public void Poll(SDL_Event* eventPtr)
        {

            var keyMapped = _pmdKeyMap.TryGetValue(eventPtr->key.key, out var key);
            if (!keyMapped) return;

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (eventPtr->Type)
            {
                case SDL_EVENT_KEY_DOWN:
                    KeyDown?.Invoke(key);
                    break;
                case SDL_EVENT_KEY_UP:
                    KeyUp?.Invoke(key);
                    break;
            }
        }

      
    }
}
