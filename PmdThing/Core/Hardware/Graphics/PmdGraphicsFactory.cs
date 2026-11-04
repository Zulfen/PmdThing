using PmdThing.Core.Hardware.Graphics.SDL;
using PmdThing.Core.Hardware.Graphics.SDL.GPU.OpenGL;

namespace PmdThing.Core.Hardware.Graphics
{
    public static class PmdGraphicsFactory
    {
        public static PmdGraphics? Create(int width, int height, string title)
        {
            PmdGraphics[] backends =
            [
                new SdlPmdGraphicsGl(width, height, title),
                new SdlPmdGraphicsSoft(width, height, title)
            ];

            foreach (var backend in backends)
            {
                var supportCheck = backend.GetCapabilities();

                if (supportCheck.Passed)
                {
                    return backend;
                }

                backend.Cleanup();
            }

            return null;
        }
    }
}
