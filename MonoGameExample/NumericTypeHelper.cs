using Microsoft.Xna.Framework;

namespace MonoGameExample
{
    public static class NumericTypeHelper
    {
        public static System.Numerics.Vector2 ToSystemVector2(this Vector2 v)
        {
            return new System.Numerics.Vector2(v.X, v.Y);
        }

        public static Vector2 ToXnaVector2(this System.Numerics.Vector2 v)
        {
            return new Vector2(v.X, v.Y);
        }
    }
}