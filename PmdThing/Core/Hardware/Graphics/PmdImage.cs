using System.Drawing;

namespace PmdThing.Core.Hardware.Graphics
{
    public abstract class PmdImage(PmdComponentConfig config) : PmdComponent(config)
    {

        public abstract int GetWidth();
        public abstract int GetHeight();
        // TODO: add scaling options
        public abstract void Resize(int width, int height);

        public abstract void Draw(RectangleF destRect, RectangleF? fromRect, bool centered);

        public void Draw(int x, int y, bool centered)
        {
            var imageRect = new RectangleF
            {
                X = x,
                Y = y,
                Width = GetWidth(),
                Height = GetHeight(),
            };
            Draw(imageRect, null, centered);
        }

        public abstract void Destroy();

    }
    

}
