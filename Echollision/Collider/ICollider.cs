using System.Numerics;

namespace ViLAWAVE.Echollision.Collider
{
    public interface ICollider
    {
        /// <summary>
        /// Geometry center in collider coordinate.
        /// </summary>
        Vector2 Center { get; }
        /// <summary>
        /// Get support point of normal direction in collider coordinate.
        /// </summary>
        /// <param name="normal">Support direction in collider coordinate.</param>
        /// <returns></returns>
        Vector2 Support(Vector2 normal);
    }
}
