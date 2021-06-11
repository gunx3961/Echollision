using System;
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
        internal Vector2 WorldCenter(in ColliderTransform transform)
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
        public Vector2 WorldSupport(in ColliderTransform transform, Vector2 direction)
        {
            var rotation = Matrix3x2.CreateRotation(transform.Rotation);
            Matrix3x2.Invert(rotation, out var inverted);
            var localNormal = Vector2.TransformNormal(direction, inverted);
            var supportLocal = Support(localNormal);
            var supportWorld = Vector2.Transform(supportLocal, rotation) + transform.Translation;
            return supportWorld;
        }

        /// <summary>
        /// Initialize bounding information of this collider.<br/>
        /// Make sure to call this method at proper timing to perform broad phase detection.
        /// </summary>
        public void InitializeBounding()
        {
            // TODO: better bounding sphere computing
            var xMin = Support(new Vector2(-1, 0));
            var xMax = Support(new Vector2(1, 0));
            var yMin = Support(new Vector2(0, -1));
            var yMax = Support(new Vector2(0, 1));

            var width = xMax.X - xMin.X;
            var height = yMax.Y - yMin.Y;
            _boundingRadius = new Vector2(width, height).Length() / 2f;
            _boundingCenter = new Vector2((xMin.X + xMax.X) / 2f, (yMin.Y + yMax.Y) / 2f);
        }

        
        internal SphereSweptArea SphereSweptArea(in ColliderTransform transform, Vector2 movement)
        {
            var center = Vector2.TransformNormal(_boundingCenter, transform.Matrix());
            var scale = MathF.Max(transform.Scale.X, transform.Scale.Y);
            return new SphereSweptArea(center, center + movement, _boundingRadius * scale);
        }

        private float _boundingRadius;
        private Vector2 _boundingCenter;
    }
}
