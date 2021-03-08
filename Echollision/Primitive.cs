using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public readonly struct Primitive
    {
        public readonly PrimitiveType Type;
        public readonly float Rx;
        public readonly float Ry;
        public readonly float Rotation;
        public readonly Vector2 Translation;

        public Primitive(PrimitiveType type, float rx, float ry, float rotation = 0,
            Vector2 translation = new Vector2())
        {
            Type = type;
            Rx = rx;
            Ry = ry;
            Rotation = rotation;
            Translation = translation;
        }

        internal Vector2 GetCenterLegacy()
        {
            return Type switch
            {
                PrimitiveType.Sphere => Translation,
                PrimitiveType.Segment => Translation,
                PrimitiveType.Point => Translation,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal Vector2 Center => Translation;

        internal Vector2 Support(Vector2 normal)
        {
            var rotation = Matrix3x2.CreateRotation(Rotation);
            Matrix3x2.Invert(rotation, out var inverted);
            var localNormal = Vector2.TransformNormal(normal, inverted);
            var supportLocal = SupportLocal(localNormal);
            var supportWorld = Vector2.Transform(supportLocal, rotation) + Translation;
            return supportWorld;
        }

        private Vector2 SupportLocal(Vector2 normal)
        {
            return Type switch
            {
                PrimitiveType.Sphere => Rx * normal,
                PrimitiveType.Segment => new Vector2(Math.Sign(normal.X) * Rx, 0),
                PrimitiveType.Point => Vector2.Zero,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum PrimitiveType
    {
        Point = 0,
        Sphere = 1,
        Segment = 2,
    }
}