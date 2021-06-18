namespace MonoGameExample.Ecs
{
    public struct Life
    {
        public float Length;
        public float BirthTime;

        public float DeathTime => BirthTime + Length;
    }
}
