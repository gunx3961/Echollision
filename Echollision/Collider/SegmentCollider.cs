using System.Numerics;

namespace ViLAWAVE.Echollision.Collider
{
    public class SegmentCollider : ICollider
    {
        /// <summary>
        /// Create a line segment collider of specific endpoints.
        /// </summary>
        /// <param name="a">Endpoint A.</param>
        /// <param name="b">Endpoint B.</param>
        public SegmentCollider(Vector2 a, Vector2 b)
        {
            _a = a;
            _b = b;
        }

        public Vector2 Center => (_a + _b) / 2f;

        public Vector2 Support(Vector2 normal)
        {
            return Vector2.Dot(_b - _a, normal) > 0 ? _b : _a;
        }

        private readonly Vector2 _a;
        private readonly Vector2 _b;
    }
}