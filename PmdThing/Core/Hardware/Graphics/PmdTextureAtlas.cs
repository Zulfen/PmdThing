using System.Drawing;
using PmdThing.Core.Util;
using RectpackSharp;

namespace PmdThing.Core.Hardware.Graphics
{
    /// <summary>
    /// A global atlas used for all runtime textures.
    /// Useful for sending sprites to the GPU in one or a handful of passes
    /// </summary>
    /// <typeparam name="TI">Image</typeparam>
    public abstract class PmdTextureAtlas<TI>(PmdComponentConfig config) : PmdComponent(config) where TI : PmdImage
    {
        public abstract Point AddTexture(TI image);

        public abstract void RemoveTexture(TI image);

        public abstract void Stitch();

        public abstract void Use(TI image);
        public abstract int GetAtlasWidth(TI image);
        public abstract int GetAtlasHeight(TI image);
    }
}
