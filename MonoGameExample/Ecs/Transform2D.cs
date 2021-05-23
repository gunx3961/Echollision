using System.Numerics;
using ViLAWAVE.Echollision;

namespace MonoGameExample.Ecs
{
    public struct Transform2D
    {
        public Vector2 Position;
        public float Rotation;

        public Transform ToCollisionTransform()
        {
            return new Transform(Position, Rotation);
        }
    }
}