using PmdThing.Core.Hardware.Framework.SDL;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Hardware.Graphics.SDL;
using PmdThing.Core.Hardware.Input;
using PmdThing.Core.Hardware.Input.SDL;

namespace PmdThing.Core.Hardware.Framework
{
    public abstract class PmdFramework(PmdGraphics graphics, IPmdKeyboard keyboard)
    {
        public PmdGraphics Graphics = graphics;
        public IPmdKeyboard Keyboard = keyboard;

        private long _running = 1;
        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        // update framework stuff
        public abstract void Init();
        public abstract void Shutdown();
        public abstract void Update();
    }
}
