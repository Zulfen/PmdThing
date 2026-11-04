using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PmdThing.Core.Hardware.Graphics
{

    // TODO: multiple windows?
    public abstract class PmdGraphics(int windowWidth, int windowHeight, string windowTitle)
    {
       public abstract bool LoadImage(string path, out PmdImage image);
       public abstract void DrawLine(float x1, float y1, float x2, float y2);
       public abstract void DrawRectangle(PmdRect rect);
       public abstract float GetDelta();
       public abstract PmdWindow GetMainWindow();
       public abstract PmdWindow CreateWindow(int width, int height);
       public abstract void PreUpdate();
       public abstract void PreRender();
       public abstract void PostRender();
       public abstract void Cleanup();
    }

}
