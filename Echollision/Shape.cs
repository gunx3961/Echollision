using System;

namespace ViLAWAVE.Echollision
{
    public readonly ref struct Shape
    {
        public readonly ShapeType Type;
        public readonly Span<Primitive> Primitives;

        public Shape(ShapeType type, Span<Primitive> primitives)
        {
            Type = type;
            Primitives = primitives;
        }
    }

    public enum ShapeType
    {
        Primitive = 0,
        MinkowskiSum = 1,
    }
}