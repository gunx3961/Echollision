using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public class SphereCollider : Collider
    {
        /// <summary>
        /// Create a sphere collider in specific radius.
        /// </summary>
        /// <param name="radius">Can be 0 to represent a point collider.</param>
        public SphereCollider(float radius)
        {
            _radius = radius;
        }
        
        internal override Vector2 Center()
        {
            return Vector2.Zero;
        }

        internal override Vector2 Support(Vector2 direction)
        {
            return _radius * Vector2.Normalize(direction);
        }

        private readonly float _radius;
    }
}