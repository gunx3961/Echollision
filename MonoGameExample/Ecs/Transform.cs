using System.Numerics;
using ViLAWAVE.Echollision;

namespace MonoGameExample.Ecs
{
    public struct Transform
    {
        public Transform(Vector2 position, float rotation = 0f)
        {
            Position = DestinationPosition = position;
            Rotation = DestinationRotation = rotation;
        }

        public Vector2 Position;
        public float Rotation;
        public Vector2 DestinationPosition;
        public float DestinationRotation;

        public Vector2 Movement => DestinationPosition - Position;

        public ColliderTransform ToColliderTransform()
        {
            return new ColliderTransform(Position, Rotation);
        }

        public void ApplyDestination()
        {
            Position = DestinationPosition;
            Rotation = DestinationRotation;
        }
    }
}