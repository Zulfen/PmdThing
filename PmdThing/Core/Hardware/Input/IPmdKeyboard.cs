namespace PmdThing.Core.Hardware.Input
{

    public class PmdKeyEventArgs(PmdKey key) : EventArgs
    {
        public PmdKey Key { get; } = key;
    }

    public interface IPmdKeyboard
    {
        public delegate void OnKey(PmdKey key);
        public event OnKey KeyDown;
        public event OnKey KeyUp;
        bool IsKeyDown(PmdKey key);
    }

    public enum PmdKey
    {
        LeftArrow,
        RightArrow,
        DownArrow,
        UpArrow,
    }

}
