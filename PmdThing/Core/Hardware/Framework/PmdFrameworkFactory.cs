using Microsoft.Extensions.Logging;
using PmdThing.Core.Hardware.Framework.Headless;
using PmdThing.Core.Hardware.Framework.SDL;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Hardware.Graphics.Headless;
using PmdThing.Core.Hardware.Graphics.SDL;
using PmdThing.Core.Hardware.Input.Headless;
using PmdThing.Core.Hardware.Input.SDL;

namespace PmdThing.Core.Hardware.Framework
{
    public static class PmdFrameworkFactory
    {


        public static PmdFramework Create(int width, int height, string title, string[] commandLineArgs)
        {
            var graphics = PmdGraphicsFactory.Create(width, height, title, commandLineArgs);

            // the framework is quite tied to the graphics api at the moment, hence below
            PmdFramework framework;
            switch (graphics)
            {
                case SdlPmdGraphicsBase sdlGraphics:
                {
                    var sdlKeyboard = new SdlPmdKeyboard();
                    framework = new SdlPmdFramework(sdlGraphics, sdlKeyboard);
                    break;
                }
                case PmdHeadlessGraphics headlessGraphics:
                {
                    var headlessKb = new PmdHeadlessKeyboard();
                    framework = new PmdHeadlessFramework(headlessGraphics, headlessKb);
                    break;
                }
                default:
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
