using System;
using System.Numerics;

namespace ViLAWAVE.Echollision.BroadPhase
{
    public struct SweptCapsule
    {
        internal SweptCapsule(Vector2 a, Vector2 b, float r)
        {
            A = a;
            B = b;
            R = r;
        }

        internal Vector2 A;
        internal Vector2 B;
        internal float R;
        
        internal static bool Intersection(ref SweptCapsule a, ref SweptCapsule b)
        {
            var distanceSquared = DistanceSquaredSegmentSegment(ref a.A, ref a.B, ref b.A, ref b.B);
            var r = a.R + b.R;
            return distanceSquared <= r * r;
        }

        private static float DistanceSquaredSegmentSegment(ref Vector2 p1, ref Vector2 q1, ref Vector2 p2,
            ref Vector2 q2)
        {
            float s, t;

            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;

            var a = d1.LengthSquared();
            var e = d2.LengthSquared();
            var f = Vector2.Dot(d2, r);

            var aIsZero = a <= float.Epsilon;
            var eIsZero = e <= float.Epsilon;
            // Both segments degenerate into points
            if (aIsZero && eIsZero)
            {
                var p2p1 = p1 - p2;
                return p2p1.LengthSquared();
            }

            // First segment degenerates into a point
            if (aIsZero)
            {
                s = 0f;
                t = Math.Clamp(f / e, 0f, 1f);
            }
            else
            {
                var c = Vector2.Dot(d1, r);

                // Second segment degenerates into a point
                if (eIsZero)
                {
                    t = 0f;
                    s = Math.Clamp(-c / a, 0f, 1f);
                }
                else
                {
                    // General case
                    var b = Vector2.Dot(d1, d2);
                    var denom = a * e - b * b;

                    s = denom != 0f ? Math.Clamp((b * f - c * e) / denom, 0f, 1f) : 0f;

                    t = (b * s + f) / e;

                    if (t < 0f)
                    {
                        t = 0f;
                        s = Math.Clamp(-c / a, 0f, 1f);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Math.Clamp((b - c) / a, 0f, 1f);
                    }
                }
            }

            var c1 = p1 + d1 * s;
            var c2 = p2 + d2 * t;
            var c2c1 = c1 - c2;
            return c2c1.LengthSquared();
        }
    }
}