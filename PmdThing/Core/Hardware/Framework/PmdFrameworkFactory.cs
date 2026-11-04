using Microsoft.Extensions.Logging;
using PmdThing.Core.Hardware.Framework.SDL;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Hardware.Graphics.SDL;
using PmdThing.Core.Hardware.Input.SDL;

namespace PmdThing.Core.Hardware.Framework
{
    public static class PmdFrameworkFactory
    {


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

            if (!capabilities.Headless)
            {
                framework.Logger.LogInformation("   Hardware API: {api} ({version})", capabilities.Api, capabilities.ApiVersion ?? "N/A");
            }
            
            framework.Init();
            return framework;
        }

    }
}
