using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public class SegmentCollider : Collider
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

        internal override Vector2 Center()
        {
            return (_a + _b) / 2f;
        }

        internal override Vector2 Support(Vector2 direction)
        {
            return Vector2.Dot(_b - _a, direction) > 0 ? _b : _a;
        }

        private readonly Vector2 _a;
        private readonly Vector2 _b;
    }
}