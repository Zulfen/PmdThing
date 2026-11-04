using SDL;
using static SDL.SDL3;
using static SDL.SDL_EventType;

namespace PmdThing.Core.Hardware.Input.SDL
{
    public unsafe class SdlPmdKeyboard : IPmdKeyboard
    {

        public event IPmdKeyboard.OnKey? KeyDown;
        public event IPmdKeyboard.OnKey? KeyUp;

        private readonly Dictionary<SDL_Keycode, PmdKey> _sdlKeyMap = new()
        {
            { SDL_Keycode.SDLK_UP, PmdKey.UpArrow },
            { SDL_Keycode.SDLK_DOWN, PmdKey.DownArrow },
            { SDL_Keycode.SDLK_LEFT, PmdKey.LeftArrow },
            { SDL_Keycode.SDLK_RIGHT, PmdKey.RightArrow },
        };

        private readonly Dictionary<PmdKey, bool> _keyStateMap = new();

        public SdlPmdKeyboard()
        {
            foreach (var pmdKey in _sdlKeyMap.Values)
            {
                _keyStateMap[pmdKey] = false;
            }
        }

        public bool IsKeyDown(PmdKey key)
        {
            return _keyStateMap.TryGetValue(key, out var down) && down;
        }


        public void Poll(SDL_Event* eventPtr)
        {

            var keyMapped = _sdlKeyMap.TryGetValue(eventPtr->key.key, out var key);
            if (!keyMapped) return;

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (eventPtr->Type)
            {
                case SDL_EVENT_KEY_DOWN:
                    _keyStateMap[key] = true;
                    KeyDown?.Invoke(key);
                    break;
                case SDL_EVENT_KEY_UP:
                    _keyStateMap[key] = false;
                    KeyUp?.Invoke(key);
                    break;
            }
        }

    }
}
