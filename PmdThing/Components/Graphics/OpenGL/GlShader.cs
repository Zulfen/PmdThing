using GlmSharp;
using OpenTK.Graphics.OpenGL;
using PmdThing.Core;
using PmdThing.Core.Hardware.Graphics;

namespace PmdThing.Components.Graphics.OpenGL
{
    public class GlShader : Shader
    {

        public int ProgramId { get; init; }
        public int VertexShader { get; init; }
        public int FragmentShader { get; init; }

        public int VertexArrayObject { get; init; }
        public int VertexBufferObject { get; init; }

        //public int ElementBufferObject { get; private set; } = GL.GenBuffer();

        public string DataAttribute { get; init; }

        private readonly Dictionary<string, int> _uniformLocations = [];

        private readonly List<float> _gpuUploadVertex = [];

        public GlShader(PmdComponentConfig config, string vertexPath, string fragmentPath, string dataAttrib = "") : base(config, vertexPath, fragmentPath)
        {
            if (!Base.IsMainThread())
            {
                throw new InvalidOperationException("Shaders must be created on the main thread.");
            }

            DataAttribute = dataAttrib;

            RegisterIfNotExist<GlProgram>(true, out var glProgram);
            ProgramId = glProgram.Handle;

            VertexShader = GL.CreateShader(ShaderType.VertexShader);
            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);

            VertexArrayObject = GL.GenVertexArray();
            VertexBufferObject = GL.GenBuffer();

            CompileAndLink(VertexShader, VertexShaderString, FragmentShader, FragmentShaderString);

            CheckError();

            // TODO: make this not hardcoded
            SetupBuffers();
        }

        private static void CheckError()
        {
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                throw new Exception("GL error:" + error);
            }
        }

        private void SetupBuffers()
        {
            GL.BindVertexArray(VertexArrayObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            GL.EnableVertexAttribArray(0);
        }

        private void PushVerticesGpu()
        {
            // Binding again in here to be explicit so we definitely change the right thing
            GL.BindVertexArray(VertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _gpuUploadVertex.Count * sizeof(float), _gpuUploadVertex.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);
        }

        private void CompileAndLink(int vertexId, string shaderString, int fragId, string fragString)
        {
            // compile vertex shader
            GL.ShaderSource(vertexId, shaderString);
            GL.CompileShader(vertexId);
            GL.GetShader(vertexId, ShaderParameter.CompileStatus, out var vertexStatus);

            if (vertexStatus <= 0)
            {
                var compileLog = GL.GetShaderInfoLog(vertexId);
                throw new ArgumentException($"GLSL vertex shader failed to compile: {compileLog}");
            }

            // compile frag shader
            GL.ShaderSource(fragId, fragString);
            GL.CompileShader(fragId);
            GL.GetShader(fragId, ShaderParameter.CompileStatus, out var fragStatus);

            if (fragStatus <= 0)
            {
                var compileLog = GL.GetShaderInfoLog(vertexId);
                throw new ArgumentException($"GLSL fragment shader failed to compile: {compileLog}");
            }

            GL.AttachShader(ProgramId, vertexId);
            GL.AttachShader(ProgramId, fragId);

            GL.LinkProgram(ProgramId);

            GL.GetProgram(ProgramId, GetProgramParameterName.LinkStatus, out var linkStatus);
            var infoLog = GL.GetProgramInfoLog(ProgramId);
            if (linkStatus <= 0)
            {
                throw new ArgumentException($"GLSL program failed to link: {infoLog}");
            }

            //GL.DetachShader(ProgramId, vertexId);
            //GL.DeleteShader(vertexId);

            //GL.DetachShader(ProgramId, fragId);
            //GL.DeleteShader(fragId);

            // Get uniforms / vars from shader
            GL.GetProgram(ProgramId, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            for (var i = 0; i < numberOfUniforms; i++)
            {
                // get the name of this uniform,
                var key = GL.GetActiveUniform(ProgramId, i, out _, out _);

                // get the location,
                var location = GL.GetUniformLocation(ProgramId, key);

                // and then add it to the dictionary.
                _uniformLocations.Add(key, location);
            }
        }

        public override void AddToBuffer(float[] vertices)
        {
            _gpuUploadVertex.AddRange(vertices);
            PushVerticesGpu();
        }

        public override bool IsBufferEmpty()
        {
            return _gpuUploadVertex.Count == 0;
        }

        public override void Use()
        {
            GL.BindVertexArray(VertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.UseProgram(ProgramId);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(ProgramId, attribName);
        }

        public override void SetInt(string name, int data)
        {
            GL.Uniform1(_uniformLocations[name], data);
        }

        public override void SetFloat(string name, float data)
        {
            GL.Uniform1(_uniformLocations[name], data);
        }

        public override void SetVec2(string name, vec2 data)
        {
            GL.Uniform2(_uniformLocations[name], data.x, data.y);
        }

        public void SetVec3(string name, vec3 data)
        {
            GL.Uniform3(_uniformLocations[name], data.x, data.y, data.z);
        }

        public void SetVec4(string name, vec4 data)
        {
            GL.Uniform4(_uniformLocations[name], data.x, data.y, data.z, data.w);
        }

        public override void SetMat4(string name, mat4 data)
        {
            GL.UniformMatrix4(_uniformLocations[name], 1, false, data.Values1D);
        }

        public override void Cleanup()
        {
            GL.DeleteProgram(ProgramId);
        }
    }
}
