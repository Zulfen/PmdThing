using Microsoft.Extensions.Logging;
using PmdThing.Core.Hardware.Framework.SDL;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Hardware.Graphics.SDL;
using PmdThing.Core.Hardware.Input.SDL;

namespace PmdThing.Core.Hardware.Framework
{
    public static class PmdFrameworkFactory
    {

        private static PmdFramework? _instance;


        public static PmdFramework Create(int width, int height, string title)
        {
            var graphics = PmdGraphicsFactory.Create(width, height, title);

            // the framework is quite tied to the graphics api at the moment, hence below
            PmdFramework framework;
            if (graphics is SdlPmdGraphicsBase sdlGraphics)
            {
                var sdlKeyboard = new SdlPmdKeyboard();
                framework = new SdlPmdFramework(sdlGraphics, sdlKeyboard);
            }
            else
            {
                throw new PlatformNotSupportedException("what??");
            }

            var capabilities = framework.Graphics.GetCapabilities();

            framework.Logger.LogInformation("Using framework {framework} with capabilities:", framework);
            framework.Logger.LogInformation("   Hardware API: {api} ({version})", capabilities.Api, capabilities.ApiVersion);

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
