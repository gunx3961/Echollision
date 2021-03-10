using System;
using System.Numerics;
using ViLAWAVE.Echollision.Collider;

namespace ViLAWAVE.Echollision
{
    public static class Collision
    {
        public static bool Detect(ICollider a, in Transform transformA, ICollider b, in Transform transformB)
        {
            DebugDraw.OnDrawString("origin", Vector2.Zero);
            DebugDraw.OnDrawPoint(Vector2.Zero);

            var centerA = a.Center;
            var centerB = b.Center;
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) v0 = new Vector2(0.00001f, 0);

            DebugDraw.OnDrawString("v0", v0);
            DebugDraw.OnDrawPoint(v0);
            var normal = Vector2.Normalize(-v0);
            DebugDraw.OnDrawString("origin ray", v0 + normal * 240);
            DebugDraw.OnDrawLine(v0, v0 + normal * 240);

            var v1 = SupportOfMinkowskiDifference(a, transformA, b, transformB, normal);
            DebugDraw.OnDrawString("v1", v1);
            DebugDraw.OnDrawPoint(v1);
            DebugDraw.OnDrawLine(v0, v1);
            normal = Vector2.Normalize(v1 - v0);
            normal = new Vector2(normal.Y, -normal.X);
            if (Vector2.Dot(-v0, normal) < 0) normal = -normal;

            var v2 = SupportOfMinkowskiDifference(a, transformA, b, transformB, normal);
            DebugDraw.OnDrawString("v2", v2);
            DebugDraw.OnDrawPoint(v2);
            DebugDraw.OnDrawLine(v0, v2);
            DebugDraw.OnDrawLine(v1, v2);

            var counter = MaxRefinement;
            while (counter > 0)
            {
                normal = Vector2.Normalize(v2 - v1);
                normal = new Vector2(normal.Y, -normal.X);
                if (Vector2.Dot(normal, v0 - v1) > 0) normal = -normal; // Outer normal

                var debugMidPoint = (v1 + v2) / 2;
                DebugDraw.OnDrawLine(debugMidPoint, debugMidPoint + normal * 100);
                DebugDraw.OnDrawString("n", debugMidPoint + normal * 100);

                if (Vector2.Dot(normal, -v1) < 0) return true;

                var v3 = SupportOfMinkowskiDifference(a, transformA, b, transformB, normal);
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

                counter -= 1;
            }

            return false;
        }
        
        private const int MaxRefinement = 5;
        
        public static Vector2 WorldSupport(this ICollider shape, in Transform transform, Vector2 normal)
        {
            var rotation = Matrix3x2.CreateRotation(transform.Rotation);
            Matrix3x2.Invert(rotation, out var inverted);
            var localNormal = Vector2.TransformNormal(normal, inverted);
            var supportLocal = shape.Support(localNormal);
            var supportWorld = Vector2.Transform(supportLocal, rotation) + transform.Translation;
            return supportWorld;
        }

        public static Vector2 SupportOfMinkowskiDifference(ICollider a, in Transform ta, ICollider b, in Transform tb,
            Vector2 normal)
        {
            return b.WorldSupport(tb, normal) - a.WorldSupport(ta, -normal);
        }


    }
}