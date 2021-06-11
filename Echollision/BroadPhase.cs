using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    internal static class BroadPhase
    {
        internal static bool Intersection(ref SphereSweptArea a, ref SphereSweptArea b)
        {
            return false;
        }

        private static float DistanceSquaredSegmentSegment(ref Vector2 p1,  ref Vector2 q1, ref Vector2 p2, ref Vector2 q2)
        {
            // var d1 = q1 - p1;
            // var d2 = q2 - p2;
            //
            // d2.LengthSquared();
            return 0f;
        }
    }
}
