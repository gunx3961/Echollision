using System.Numerics;

namespace ViLAWAVE.Echollision
{
    internal struct SphereSweptArea
    {
        internal SphereSweptArea(Vector2 a, Vector2 b, float r)
        {
            A = a;
            B = b;
            R = r;
        }

        internal Vector2 A;
        internal Vector2 B;
        internal float R;
    }
}