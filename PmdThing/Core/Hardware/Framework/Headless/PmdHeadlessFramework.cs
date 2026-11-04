using PmdThing.Core.Hardware.Graphics.Headless;
using PmdThing.Core.Hardware.Input.Headless;

namespace PmdThing.Core.Hardware.Framework.Headless
{
    public class PmdHeadlessFramework(PmdHeadlessGraphics graphics, PmdHeadlessKeyboard keyboard) : PmdFramework(graphics, keyboard)
    {
        public override void Init()
        {
        }

        public override void Shutdown()
        {
        }

        public override void Update()
        {
        }

        public override string ToString()
        {
            return "Server";
        }
    }
}
