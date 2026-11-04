using System.Drawing;

namespace PmdThing.Core.Hardware.Graphics.Headless
{
    public class PmdHeadlessGraphics(int windowWidth, int windowHeight, string windowTitle)
        : PmdGraphics(windowWidth, windowHeight, windowTitle)
    {
        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override PmdWindow CreateWindow(int width, int height)
        {
            throw new NotImplementedException();
        }

        public override Capabilities GetCapabilities()
        {
            return new Capabilities
            {
                Headless = true,
                HardwareTransformations = false,
                Api = HardwareApi.None,
                Passed = true
            };
        }

        public override float GetDelta()
        {
            throw new NotImplementedException();
        }

        public override PmdWindow GetMainWindow()
        {
            throw new NotImplementedException();
        }

        public override PmdImage LoadImage(PmdComponent parent, string path)
        {
            throw new NotImplementedException();
        }

        public override void PostRender()
        {
        }

        public override void PreRender()
        {
        }

        public override void PreUpdate()
        {
        }
    }
}
