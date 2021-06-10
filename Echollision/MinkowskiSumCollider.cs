using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public class MinkowskiSumCollider : Collider
    {
        public MinkowskiSumCollider(Collider[] shapes)
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
            return center;
        }

        internal override Vector2 Support(Vector2 direction)
        {
            var support = _shapes[0].Support(direction);
            for (var i = 1; i < _shapes.Length; i += 1)
            {
                support += _shapes[i].Support(direction);
            }
            return support;
        }

        private readonly Collider[] _shapes;
    }
}