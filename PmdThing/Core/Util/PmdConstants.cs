using GlmSharp;
using PmdThing.Components;

namespace PmdThing.Core.Util
{
    public class PmdConstants
    {

        public static vec2 UpVector = new(0, -1);

        public enum ShaderType
        {
            Vertex,
            Fragment
        }

        public enum Pokemon
        {
            Vulpix,
            Eevee
        }

        public enum AnimationType
        {
            Attack,
            Charge,
            Cringe,
            DeepBreath,
            Double,
            Eat,
            EventSleep,
            Faint,
            Float,
            Head,
            HitGround,
            Hop,
            Hurt,
            Idle,
            Laying,
            LeapForth,
            LookUp,
            LostBalance,
            Nod,
            Pain,
            Pose,
            Pull,
            RearUp,
            Rotate,
            Shoot,
            Sink,
            Sit,
            Sleep,
            Swing,
            Trip,
            Tumble,
            TumbleBack,
            Wake,
            Walk,
            TailWhip
        }

        public class PmdAnimation
        {
            public int[] Durations { get; set; } = [];
            public required SpriteSheet SpriteSheet { get; set; }

            public int OffsetX { get; set; }
            public int OffsetY { get; set; }
        }

        public enum AnimationDirection
        {
            South,
            SouthEast,
            East,
            NorthEast,
            North,
            NorthWest,
            West,
            SouthWest
        }

        public static vec2 DirectionToVec2(AnimationDirection direction)
        {
            // animations in sprite sheet start at south
            var theta = 2 * Math.PI -  Math.PI / 4 * (int)direction - Math.PI;

            return new vec2((float)Math.Sin(theta), (float)Math.Cos(theta) * -1).NormalizedSafe;
        }

        public static double CalculateAngle(vec2 direction)
        {
            var theta = (direction * UpVector).Angle - Math.PI / 2 + Math.PI;
            if (theta < 0) theta += 2 * Math.PI;
            return theta;
        }

        public static AnimationDirection Vec2ToDirection(vec2 direction)
        {
            // animations in sprite sheet start at south
            var theta = CalculateAngle(direction);

            var index = (int)(theta / (Math.PI / 4));
            return (AnimationDirection) index;

        }

        public static PmdTileRef Vec2ToTile(vec2 position)
        {
            return new PmdTileRef((int)position.y, (int)position.x);
        }

    }
}
