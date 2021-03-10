using System.Numerics;

namespace ViLAWAVE.Echollision
{
    /// <summary>
    /// Represent a 2D transform apply to collider.
    /// </summary>
    public readonly ref struct Transform
    {
        public Transform(Vector2 translation, float rotation)
        {
            Translation = translation;
            Rotation = rotation;
        }
        
        public readonly Vector2 Translation;
        public readonly float Rotation;
    }
}