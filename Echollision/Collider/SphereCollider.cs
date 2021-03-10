using System.Numerics;

namespace ViLAWAVE.Echollision.Collider
{
    public class SphereCollider : ICollider
    {
        /// <summary>
        /// Create a sphere collider in specific radius.
        /// </summary>
        /// <param name="radius">Can be 0 to represent a point collider.</param>
        public SphereCollider(float radius)
        {
            _radius = radius;
        }
        
        public Vector2 Center => Vector2.Zero;

        public Vector2 Support(Vector2 normal)
        {
            return _radius * normal;
        }

        private readonly float _radius;
    }
}