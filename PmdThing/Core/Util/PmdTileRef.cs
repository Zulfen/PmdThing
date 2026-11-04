using GlmSharp;
using PmdThing.Core.Network.Rokon;

namespace PmdThing.Core.Util
{

    [PmdSerializable]
    public record PmdTileRef(int Row, int Column)
    {
        public int Row { get; } = Row;
        public int Column { get; } = Column;
        public bool Valid { get; set; } = true;

        public static PmdTileRef operator -(PmdTileRef left, PmdTileRef right)
        {
            return new PmdTileRef(left.Row - right.Row, left.Column - right.Column);
        }

        public vec2 ToVec2()
        {
            return new vec2(Column, Row);
        }

    }
}
