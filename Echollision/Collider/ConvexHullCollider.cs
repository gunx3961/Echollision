using System.Numerics;

namespace ViLAWAVE.Echollision.Collider
{
    public class ConvexHullCollider : ICollider
    {
        public ConvexHullCollider(ICollider[] shapes)
        {
            _shapes = shapes;
        }
        
        public Vector2 Center
        {
            get
            {
                var center = _shapes[0].Center;
                for (var i = 1; i < _shapes.Length; i += 1)
                {
                    center += _shapes[i].Center;
                }

                return center / _shapes.Length;
            }
        }
        
        public Vector2 Support(Vector2 normal)
        {
            var support = _shapes[0].Support(normal);
            var max = support;

            for (var i = 1; i < _shapes.Length; i += 1)
            {
                support = _shapes[i].Support(normal);
                if (Vector2.Dot(normal, support - max) > 0) max = support;
            }

            return max;
        }

        private readonly ICollider[] _shapes;
    }
}