using System.Numerics;

namespace ViLAWAVE.Echollision
{
    /// <summary>
    /// The base class of all collider.
    /// </summary>
    public abstract class Collider
    {
        /// <summary>
        /// Defines the geometry center of this collider in object coordinate.
        /// </summary>
        /// <returns>The geometry center.</returns>
        internal abstract Vector2 Center();
        
        /// <summary>
        /// The support function that implicitly defines the shape of this collider in object coordinate.
        /// </summary>
        /// <param name="direction">The support direction in object coordinate.</param>
        /// <returns>The support point.</returns>
        internal abstract Vector2 Support(Vector2 direction);
        
        /// <summary>
        /// Returns the center of collider in world coordinate.
        /// </summary>
        /// <param name="transform">The transform applied to collider.</param>
        /// <returns>The center.</returns>
        internal Vector2 WorldCenter(in Transform transform)
        {
            var rotation = Matrix3x2.CreateRotation(transform.Rotation);
            return Vector2.Transform(Center(), rotation) + transform.Translation;
        }

        // TODO: make it internal
        /// <summary>
        /// Returns the support point in world coordinate.
        /// </summary>
        /// <param name="transform">The transform applied to collider.</param>
        /// <param name="direction">The support direction in world coordinate.</param>
        /// <returns>The support point.</returns>
        public Vector2 WorldSupport(in Transform transform, Vector2 direction)
        {
            var rotation = Matrix3x2.CreateRotation(transform.Rotation);
            Matrix3x2.Invert(rotation, out var inverted);
            var localNormal = Vector2.TransformNormal(direction, inverted);
            var supportLocal = Support(localNormal);
            var supportWorld = Vector2.Transform(supportLocal, rotation) + transform.Translation;
            return supportWorld;
        }
    }
}
