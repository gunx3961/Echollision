using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using ViLAWAVE.Echollision.BroadPhase;

[assembly: InternalsVisibleTo("Test")]
[assembly: InternalsVisibleTo("MonoGameExample")]
[assembly: InternalsVisibleTo("Benchmark")]
namespace ViLAWAVE.Echollision
{
    /// <summary>
    /// The base class of all collider.
    /// </summary>
    public abstract class Collider
    {
        private float _boundingRadius;
        private Vector2 _boundingCenter;
        
        
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

        /// <summary>
        /// Returns the support point in world coordinate.
        /// </summary>
        /// <param name="transform">The transform applied to collider.</param>
        /// <param name="direction">The support direction in world coordinate.</param>
        /// <returns>The support point.</returns>
        internal Vector2 WorldSupport(in ColliderTransform transform, Vector2 direction)
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

        public BoundingSphere BoundingSphere(in ColliderTransform transform)
        {
            var center = Vector2.Transform(_boundingCenter, transform.Matrix());
#if NETSTANDARD2_0
            var scale = Math.Max(transform.Scale.X, transform.Scale.Y);
#else
            var scale = MathF.Max(transform.Scale.X, transform.Scale.Y);
#endif
            return new BoundingSphere {Center = center, Radius = _boundingRadius * scale};
        }

        public Aabb BoundingBox(in ColliderTransform transform)
        {
            var center = Vector2.Transform(_boundingCenter, transform.Matrix());
            var b = new Aabb {From = center, To = center};
            b.From.X -= _boundingRadius;
            b.From.Y -= _boundingRadius;
            b.To.X += _boundingRadius;
            b.To.Y += _boundingRadius;

            return b;
        }

        public SweptCapsule SweptCapsule(in ColliderTransform transform, Vector2 movement)
        {
            var center = Vector2.TransformNormal(_boundingCenter, transform.Matrix());
#if NETSTANDARD2_0
            var scale = Math.Max(transform.Scale.X, transform.Scale.Y);
#else
            var scale = MathF.Max(transform.Scale.X, transform.Scale.Y);
#endif
            return new SweptCapsule(center, center + movement, _boundingRadius * scale);
        }

        public Aabb SweepBox(in ColliderTransform transform, Vector2 movement)
        {
            var center = Vector2.Transform(_boundingCenter, transform.Matrix());
            var b = new Aabb {From = center, To = center};
            if (movement.X > 0f)
            {
                b.From.X -= _boundingRadius;
                b.To.X += movement.X + _boundingRadius;
            }
            else
            {
                b.From.X += movement.X - _boundingRadius;
                b.To.X += _boundingRadius;
            }

            if (movement.Y > 0f)
            {
                b.From.Y -= _boundingRadius;
                b.To.Y += movement.Y + _boundingRadius;
            }
            else
            {
                b.From.Y += movement.Y - _boundingRadius;
                b.To.Y += _boundingRadius;
            }

            return b;
        }
    }
}
