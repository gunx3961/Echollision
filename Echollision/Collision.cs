using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public static class Collision
    {
        // /// <summary>
        // /// Returns the support point of primitive in normal direction.
        // /// </summary>
        // private static Vector2 Support(in Primitive primitive, ref Vector2 normal)
        // {
        //     var rotation = Matrix3x2.CreateRotation(primitive.Rotation);
        //     Matrix3x2.Invert(rotation, out var inverted);
        //     var localNormal = Vector2.TransformNormal(normal, inverted);
        //
        //     var supportLocal = primitive.Type switch
        //     {
        //         PrimitiveType.Sphere => SphereSupportLocal(primitive, localNormal),
        //         PrimitiveType.Segment => SegmentSupportLocal(primitive, localNormal),
        //         PrimitiveType.Point => Vector2.Zero,
        //         _ => throw new ArgumentOutOfRangeException()
        //     };
        //
        //     var supportWorld = Vector2.Transform(supportLocal, rotation) + primitive.Translation;
        //     return supportWorld;
        //     return Vector2.Zero;
        // }
        
        // public static Detect()

        /// <summary>
        /// Returns the support point of Minkowski sum of primitives in normal direction.
        /// </summary>
        private static Vector2 SupportOfMinkowskiSum(in Span<Primitive> primitives, ref Vector2 normal)
        {
            return Vector2.Zero;
        }
        
        /// <summary>
        /// Returns the max support point of primitives in normal direction.<br/>
        /// A max support point is the farthest support point of each primitive along the direction vector.
        /// </summary>
        private static Vector2 MaxSupport(in Span<Primitive> primitives, ref Vector2 normal)
        {
            return Vector2.Zero;
        }
    }
}