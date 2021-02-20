using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public static class MPR
    {
        public static bool Detect(Shape a, Shape b)
        {
            var centerA = a.GetCenter();
            var centerB = b.GetCenter();
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) v0 = new Vector2(0.00001f, 0);

            DebugDraw.OnDrawString("v0", v0);
            DebugDraw.OnDrawPoint(v0);
            var originRayNormal = Vector2.Normalize(-v0);
            DebugDraw.OnDrawString("origin ray", v0 + originRayNormal * 240);
            DebugDraw.OnDrawLine(v0, v0 + originRayNormal * 240);

            return false;
        }
    }
}