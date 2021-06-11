using System.Numerics;

namespace ViLAWAVE.Echollision
{
    /// <summary>
    /// Collider transform representation.
    /// </summary>
    public readonly ref struct ColliderTransform
    {
        /// <summary>
        /// Create transform by translation.
        /// </summary>
        /// <param name="translation">The position in world coordinate.</param>
        public ColliderTransform(Vector2 translation) : this(translation, 0f, Vector2.One)
        {
        }

        /// <summary>
        /// Create transform by translation and rotation.
        /// </summary>
        /// <param name="translation">The position in world coordinate.</param>
        /// <param name="rotation">The rotation in radians.</param>
        public ColliderTransform(Vector2 translation, float rotation) : this(translation, rotation, Vector2.One)
        {
        }

        /// <summary>
        /// Create transform by translation, rotation and scale.
        /// </summary>
        /// <param name="translation">The position in world coordinate.</param>
        /// <param name="rotation">The rotation in radians.</param>
        /// <param name="scale">The scale in object coordinate.</param>
        public ColliderTransform(Vector2 translation, float rotation, Vector2 scale)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = Vector2.One;
        }

        public readonly Vector2 Translation;
        public readonly float Rotation;
        public readonly Vector2 Scale;

        public Matrix3x2 Matrix()
        {
            // Scale in object coordinate first
            var m = Matrix3x2.CreateScale(Scale);
            // Apply rotation
            if (Rotation != 0f)
            {
                m *= Matrix3x2.CreateRotation(Rotation);
            }
            m.Translation = Translation;

            return m;
        }
    }
}