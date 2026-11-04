using GlmSharp;

namespace PmdThing.Core.Util
{
    public record PmdTileRef(int Row, int Column)
    {
        public int Row { get; } = Row;
        public int Column { get; } = Column;

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
