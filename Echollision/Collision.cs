using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public static class Collision
    {
        public static bool Detect<TA, TB>(in TA a, in TB b)
            where TA : struct
            where TB : struct
        {
            DebugDraw.OnDrawString("origin", Vector2.Zero);
            DebugDraw.OnDrawPoint(Vector2.Zero);

            var centerA = GetCenter(a);
            var centerB = GetCenter(b);
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) v0 = new Vector2(0.00001f, 0);

            DebugDraw.OnDrawString("v0", v0);
            DebugDraw.OnDrawPoint(v0);
            var normal = Vector2.Normalize(-v0);
            DebugDraw.OnDrawString("origin ray", v0 + normal * 240);
            DebugDraw.OnDrawLine(v0, v0 + normal * 240);

            var v1 = SupportOfMinkowskiDifference(a, b, normal);
            DebugDraw.OnDrawString("v1", v1);
            DebugDraw.OnDrawPoint(v1);
            DebugDraw.OnDrawLine(v0, v1);
            normal = Vector2.Normalize(v1 - v0);
            normal = new Vector2(normal.Y, -normal.X);
            if (Vector2.Dot(-v0, normal) < 0) normal = -normal;

            var v2 = SupportOfMinkowskiDifference(a, b, normal);
            DebugDraw.OnDrawString("v2", v2);
            DebugDraw.OnDrawPoint(v2);
            DebugDraw.OnDrawLine(v0, v2);
            DebugDraw.OnDrawLine(v1, v2);

            while (true)
            {
                normal = Vector2.Normalize(v2 - v1);
                normal = new Vector2(normal.Y, -normal.X);
                if (Vector2.Dot(normal, v0 - v1) > 0) normal = -normal; // Outer normal

                var debugMidPoint = (v1 + v2) / 2;
                DebugDraw.OnDrawLine(debugMidPoint, debugMidPoint + normal * 100);
                DebugDraw.OnDrawString("n", debugMidPoint + normal * 100);

                if (Vector2.Dot(normal, -v1) < 0) return true;

                var v3 = SupportOfMinkowskiDifference(a, b, normal);
                DebugDraw.OnDrawLine(v0, v3);

                if (Vector2.Dot(normal, v3) < 0) return false;

                normal = Vector2.Normalize(v3 - v0);
                normal = new Vector2(normal.Y, -normal.X);

                if (Vector2.Dot(v2 - v1, normal) > 0 ^ Vector2.Dot(-v0, normal) > 0) // in v1 side
                {
                    v2 = v3;
                    DebugDraw.OnDrawLine(v1, v3);
                }
                else
                {
                    v1 = v3;
                    DebugDraw.OnDrawLine(v2, v3);
                }
            }
        }

        private static Vector2 Support<T>(in T shape, Vector2 normal)
            where T : struct
        {
            return shape switch
            {
                Primitive primitive => primitive.Support(normal),
                MinkowskiSum minkowskiSum => minkowskiSum.Support(normal),
                MaxSupport maxSupport => maxSupport.Support(normal),
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unsupported shape representation.")
            };
        }

        private static Vector2 GetCenter<T>(in T shape)
            where T : struct
        {
            return shape switch
            {
                Primitive primitive => primitive.Center,
                MinkowskiSum minkowskiSum => minkowskiSum.Center,
                MaxSupport maxSupport => maxSupport.Center,
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unsupported shape representation.")
            };
        }

        private static Vector2 SupportOfMinkowskiDifference<TA, TB>(TA a, TB b, Vector2 normal)
            where TA : struct
            where TB : struct
        {
            return Support(b, normal) - Support(a, -normal);
        }
    }
}