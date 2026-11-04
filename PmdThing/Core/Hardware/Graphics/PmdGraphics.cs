namespace PmdThing.Core.Hardware.Graphics
{

    // TODO: multiple windows?
    public abstract class PmdGraphics(int windowWidth, int windowHeight, string windowTitle)
    {
        public enum HardwareApi
        {
            OpenGl,
            Vulkan,
            Direct3d,
            None
        }

        public record Capabilities
        {
            public required HardwareApi Api;
            public required bool Headless;
            public string? ApiVersion;
            public required bool Passed;
            public string? FailureReason;
            public bool HardwareTransformations; // e.g. if things like camera position are handled by passing it to a shader
        }

        public abstract PmdImage LoadImage(PmdComponent parent, string path);
        public abstract float GetDelta();
        public abstract PmdWindow GetMainWindow();
        public abstract PmdWindow CreateWindow(int width, int height);
        public abstract void PreUpdate();
        public abstract void PreRender();
        public abstract void PostRender();
        public abstract void Cleanup();
        public abstract Capabilities GetCapabilities();
    }

}
