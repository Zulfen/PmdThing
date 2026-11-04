using PmdThing.Core.Hardware.Framework.SDL;
using PmdThing.Core.Hardware.Graphics.SDL;
using PmdThing.Core.Hardware.Input.SDL;

namespace PmdThing.Core.Hardware.Framework
{
    public static class PmdFrameworkFactory
    {

        private static PmdFramework? _instance;


        public static PmdFramework Create(int width, int height, string title)
        {
            var sdlGraphics = new SdlPmdGraphics(width, height, title);
            var sdlKeyboard = new SdlPmdKeyboard();
            var framework = new SdlPmdFramework(sdlGraphics, sdlKeyboard);

            framework.Init();
            _instance = framework;
            return framework;
        }

        public static PmdFramework GetInstance()
        {
            if (_instance == null)
            {
                throw new ApplicationException(
                    "Could not get the instance of the current framework. Has it been initialised?");
            }

            return _instance;
        }

    }
}
