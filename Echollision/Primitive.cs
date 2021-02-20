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

        public Vector2 GetCenter()
        {
            return Type switch
            {
                PrimitiveType.Sphere => Translation,
                PrimitiveType.Segment => Translation,
                PrimitiveType.Point => Translation,
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