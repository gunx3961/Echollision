using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public static class CollisionLegacy
    {
        public static bool DetectDiscrete(in ShapeLegacy a, in ShapeLegacy b)
        {
            DebugDraw.OnDrawString("origin", Vector2.Zero);
            DebugDraw.OnDrawPoint(Vector2.Zero);

            var centerA = a.GetCenter();
            var centerB = b.GetCenter();
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) v0 = new Vector2(0.00001f, 0);

            DebugDraw.OnDrawString("v0", v0);
            DebugDraw.OnDrawPoint(v0);
            var normal = Vector2.Normalize(-v0);
            DebugDraw.OnDrawString("origin ray", v0 + normal * 240);
            DebugDraw.OnDrawLine(v0, v0 + normal * 240);

            var v1 = SupportMapping.SupportOfMinkowskiDifference(a, b, normal);
            DebugDraw.OnDrawString("v1", v1);
            DebugDraw.OnDrawPoint(v1);
            DebugDraw.OnDrawLine(v0, v1);
            normal = Vector2.Normalize(v1 - v0);
            normal = new Vector2(normal.Y, -normal.X);
            if (Vector2.Dot(-v0, normal) < 0) normal = -normal;

            var v2 = SupportMapping.SupportOfMinkowskiDifference(a, b, normal);
            DebugDraw.OnDrawString("v2", v2);
            DebugDraw.OnDrawPoint(v2);
            DebugDraw.OnDrawLine(v0, v2);
            DebugDraw.OnDrawLine(v1, v2);

            Vector2 v3;
            while (true)
            {
                normal = Vector2.Normalize(v2 - v1);
                normal = new Vector2(normal.Y, -normal.X);
                if (Vector2.Dot(normal, v0 - v1) > 0) normal = -normal; // Outer normal

                var debugMidPoint = (v1 + v2) / 2;
                DebugDraw.OnDrawLine(debugMidPoint, debugMidPoint + normal * 100);
                DebugDraw.OnDrawString("n", debugMidPoint + normal * 100);

                if (Vector2.Dot(normal, -v1) < 0) return true;

                v3 = SupportMapping.SupportOfMinkowskiDifference(a, b, normal);
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

        public static bool DetectContinuous(in ShapeLegacy a, Vector2 translationA, in ShapeLegacy b, Vector2 translationB)
        {
            // Combine translation of both to shape A
            // Treat shape B as a static shape 
            var translationAToB = translationA - translationB;
            
            
            return false;
        }
    }
}