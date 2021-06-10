using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public class ConvexHullCollider : Collider
    {
        public ConvexHullCollider(Collider[] shapes)
        {
            _shapes = shapes;
        }

        internal override Vector2 Center()
        {
            var center = _shapes[0].Center();
            for (var i = 1; i < _shapes.Length; i += 1)
            {
                center += _shapes[i].Center();
            }

            return center / _shapes.Length;
        }

        internal override Vector2 Support(Vector2 direction)
        {
            var support = _shapes[0].Support(direction);
            var max = support;

            for (var i = 1; i < _shapes.Length; i += 1)
            {
                support = _shapes[i].Support(direction);
                if (Vector2.Dot(direction, support - max) > 0) max = support;
            }

            return max;
        }

        private readonly Collider[] _shapes;
    }
}