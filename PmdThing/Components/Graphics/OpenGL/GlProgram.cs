using OpenTK.Graphics.OpenGL;
using PmdThing.Core;

namespace PmdThing.Components.Graphics.OpenGL
{
    public class GlProgram(PmdComponentConfig config) : PmdComponent(config)
    {
        public int Handle { get; init; } = GL.CreateProgram();

        public override void Cleanup()
        {
            GL.DeleteProgram(Handle);
        }
    }
}
