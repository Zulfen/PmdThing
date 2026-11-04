using GlmSharp;
using PmdThing.Core;
using System.Drawing;

namespace PmdThing.Components
{
    public abstract class Shader(PmdComponentConfig config, string vertexPath, string fragmentPath) : PmdComponent(config)
    {
        protected readonly string VertexShaderString = File.ReadAllText(vertexPath);
        protected readonly string FragmentShaderString = File.ReadAllText(fragmentPath);
        public abstract void Use();
        public abstract void AddToBuffer(float[] vertices);

        public abstract bool IsBufferEmpty();
        public abstract void SetInt(string name, int data);
        public abstract void SetFloat(string name, float data);
        public abstract void SetVec2(string name, vec2 data);
        public abstract void SetMat4(string name, mat4 data);
    }
}
