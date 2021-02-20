using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public static class SupportMapping
    {
        public static Vector2 Support(in Shape shape, Vector2 normal)
        {
            switch (shape.Type)
            {
                case ShapeType.Primitive:
                    return Support(shape.Primitives[0], normal);

                case ShapeType.MinkowskiSum:
                {
                    var support = Vector2.Zero;

                    for (var i = 0; i < shape.Primitives.Length; i += 1)
                    {
                        support += Support(shape.Primitives[i], normal);
                    }

                    return support;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static Vector2 Support(in Primitive primitive, Vector2 normal)
        {
            var rotation = Matrix3x2.CreateRotation(primitive.Rotation);
            Matrix3x2.Invert(rotation, out var inverted);
            var localNormal = Vector2.TransformNormal(normal, inverted);

            var supportLocal = primitive.Type switch
            {
                PrimitiveType.Sphere => SphereSupportLocal(primitive, localNormal),
                PrimitiveType.Segment => SegmentSupportLocal(primitive, localNormal),
                PrimitiveType.Point => Vector2.Zero,
                _ => throw new ArgumentOutOfRangeException()
            };

            var supportWorld = Vector2.Transform(supportLocal, rotation) + primitive.Translation;
            return supportWorld;
        }

        private static Vector2 SphereSupportLocal(in Primitive primitive, Vector2 normal)
        {
            return primitive.Rx * normal;
        }

        private static Vector2 SegmentSupportLocal(in Primitive primitive, Vector2 normal)
        {
            return new Vector2(Math.Sign(normal.X) * primitive.Rx, 0);
        }
    }
}