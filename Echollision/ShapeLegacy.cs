using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public readonly ref struct ShapeLegacy
    {
        public readonly ShapeType Type;
        public readonly Span<Primitive> Primitives;

        public ShapeLegacy(ShapeType type, Span<Primitive> primitives)
        {
            Type = type;
            Primitives = primitives;
        }

        public Vector2 GetCenter()
        {
            switch (Type)
            {
                case ShapeType.Primitive:
                    return Primitives[0].GetCenterLegacy();

                case ShapeType.MinkowskiSum:
                {
                    var center = Vector2.Zero;
                    for (var i = 0; i < Primitives.Length; i += 1)
                    {
                        center += Primitives[i].GetCenterLegacy();
                    }

                    return center;
                }

                case ShapeType.MaxSupport:
                {
                    var center = Vector2.Zero;
                    for (var i = 0; i < Primitives.Length; i += 1)
                    {
                        center += Primitives[i].GetCenterLegacy();
                    }

                    return center / Primitives.Length;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum ShapeType
    {
        Primitive = 0,
        MinkowskiSum = 1,
        MaxSupport = 2,
    }
}