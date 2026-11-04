using GlmSharp;

namespace PmdThing.Core.Util
{
    public struct PmdTileRef(int row, int column)
    {
        public int Row = row;
        public int Column = column;

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
