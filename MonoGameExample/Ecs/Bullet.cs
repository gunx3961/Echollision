using System;
using System.Numerics;

namespace MonoGameExample.Ecs
{
    public struct Bullet
    {
        public TimeSpan BirthTime;
        public Vector2 Orientation;
        public Vector2 Start;
        public bool IsTurning;
        public Vector2 TurnPoint;
        public float LifeTime;
        public float Speed;
    }
}
