using System;
using System.Numerics;

namespace MonoGameExample.Ecs
{
    public struct Bullet
    {
        public TimeSpan BirthTime;
        public Vector2 Orientation;
        public float LifeTime;
        public float Speed;
    }
}