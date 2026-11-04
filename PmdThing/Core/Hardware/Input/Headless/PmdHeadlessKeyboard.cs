namespace PmdThing.Core.Hardware.Input.Headless
{
    public class PmdHeadlessKeyboard : IPmdKeyboard
    {
        public event IPmdKeyboard.OnKey? KeyDown;
        public event IPmdKeyboard.OnKey? KeyUp;

        public bool IsKeyDown(PmdKey key)
        {
            throw new NotImplementedException();
        }
    }
}
