using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PmdThing.Core.Hardware.Graphics
{
    public abstract class PmdImage
    {

        public abstract bool Load();
        public abstract int GetWidth();
        public abstract int GetHeight();
        // TODO: add scaling options
        public abstract void Resize(int width, int height);

        public abstract void Draw(PmdRect destRect, PmdRect? fromRect, bool centered);

        public void Draw(int x, int y, bool centered)
        {
            var imageRect = new PmdRect
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


    public class PmdRect
    {
        public int Width { set; get; }
        public int Height { set; get; }
        public float X { set; get; }
        public float Y { set; get; }
    }

}
