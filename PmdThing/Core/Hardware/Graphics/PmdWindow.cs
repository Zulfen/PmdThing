namespace PmdThing.Core.Hardware.Graphics
{
    public abstract class PmdWindow(int width, int height, string title)
    {

        protected int Width = width;
        protected int Height = height;
        protected string Title = title;

        event EventHandler WindowClosed;

        public int GetWidth()
        {
            return Width;
        }

        public int GetHeight()
        {
            return Height;
        }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
            SetSizeImpl();
        }

        protected abstract void SetSizeImpl();
        public abstract void SetFullscreen(bool fullscreen);
        public abstract void Show();
        public abstract void Hide();

        public abstract void Destroy();

    }
}
