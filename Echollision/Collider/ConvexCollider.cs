using System.Numerics;

namespace ViLAWAVE.Echollision.Collider
{
    public class ConvexCollider : ICollider
    {
        public ConvexCollider(Vector2[] vertices)
        {
            _vertices = vertices;
        }

        public Vector2 Center
        {
            get
            {
                var center = _vertices[0];
                for (var i = 1; i < _vertices.Length; i += 1)
                {
                    center += _vertices[i];
                }

                return center / _vertices.Length;
            }
        }

        public Vector2 Support(Vector2 direction)
        {
            var support = _vertices[0];
            var max = support;

            for (var i = 1; i < _vertices.Length; i += 1)
            {
                support = _vertices[i];
                if (Vector2.Dot(direction, support - max) > 0) max = support;
            }

            return max;
        }

        private readonly Vector2[] _vertices;
    }
}