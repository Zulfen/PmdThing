using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using SDL;
using static SDL.SDL_GLAttr;
using static SDL.SDL3;

namespace PmdThing.Core.Hardware.Graphics.SDL.GPU.OpenGL
{
    public unsafe class SdlPmdGraphicsGl : SdlPmdGraphicsBase
    {

        private readonly bool _supportsGl3;

        public SdlPmdGraphicsGl(int width, int height, string title) : base(width, height, title, SDL_WindowFlags.SDL_WINDOW_OPENGL)
        {
            //Use OpenGL 3.3
            _supportsGl3 = SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            _supportsGl3 = SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 3);
            _supportsGl3 = SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_CORE);
            

            var contextState = SDL_GL_CreateContext(MainWindow.GetHandle());
            if (contextState == null)
            {
                _supportsGl3 = false; // no gl at all 😔
                return;
            } 

            SDL_GL_MakeCurrent(MainWindow.GetHandle(), contextState);
            GL.LoadBindings(new SdlPmdBindingsGl());

            //GL.Enable(EnableCap.DebugOutput);
            //GL.Enable(EnableCap.DebugOutputSynchronous);
            //GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            //{
            //    var msg = Marshal.PtrToStringAnsi(message, length);
            //    Console.WriteLine($"GL DEBUG: source={source} type={type} id={id} severity={severity} msg={msg}");
            //}, IntPtr.Zero);

            // NVIDIA drivers (on Windows at least) seem to force vsync on by default, so disabling it
            SDL_GL_SetSwapInterval(0);
        }

        public override void Cleanup()
        {
            MainWindow.Destroy();
        }

        public override void PreRender()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public override void PostRender()
        {
            SDL_GL_SwapWindow(MainWindow.GetHandle());
            base.PostRender();
        }

        public override PmdWindow CreateWindow(int width, int height)
        {
            throw new NotImplementedException();
        }

        public override PmdWindow GetMainWindow()
        {
            return MainWindow;
        }

        protected override SdlPmdImage CreateSdlImage(PmdComponent parent, string path)
        {
            parent.RegisterIfNotExist<SdlPmdTextureAtlasGl>(false, out _);
            parent.Register<SdlPmdImageGl>(true, out var glImage, path);
            return glImage;
        }

        public override Capabilities GetCapabilities()
        {
            return new Capabilities
            {
                Headless = false,
                Api = HardwareApi.OpenGl,
                ApiVersion = GL.GetString(StringName.Version),
                FailureReason = SDL_GetError(),
                Passed = _supportsGl3,
                ShaderSupport = true
            };
        }
    }
}
