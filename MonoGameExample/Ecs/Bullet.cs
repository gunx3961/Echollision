using System;
using System.Numerics;

namespace MonoGameExample.Ecs
{
    public struct Bullet
    {
        public TimeSpan BirthTime;
        public Vector2 Orientation;
        public Vector2 Start;
        public bool IsHit;
        public Vector2 Hit;
        public float LifeTime;
        public float Speed;
    }
}