using OpenTK;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL.GPU.OpenGL
{
    /// <summary>
    /// Provides GL function pointers to OpenTK
    /// </summary>
    public class SdlPmdBindingsGl : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
        {
            return SDL_GL_GetProcAddress(procName);
        }
    }
}
