using Microsoft.Xna.Framework;
using ViLAWAVE.Echollision;

namespace MonoGameExample
{
    public static class ColliderFactory
    {
        public static SphereCollider Point()
        {
            return new SphereCollider(0f);
        }
        
        public static ConvexCollider Rect(Vector2 size)
        {
            return Rect(size.X, size.Y);
        }

        public static ConvexCollider Rect(float width, float height)
        {
            return new ConvexCollider(new[]
            {
                System.Numerics.Vector2.Zero,
                new System.Numerics.Vector2(width, 0),
                new System.Numerics.Vector2(width, height),
                new System.Numerics.Vector2(0, height)
            });
        }
    }
}