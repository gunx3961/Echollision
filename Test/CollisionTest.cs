using System;
using Xunit;
using System.Numerics;
using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.Collider;

namespace Test
{
    public class CollisionTest
    {
        [Fact]
        public void ContinuousIllCase()
        {
            var a = new ConvexCollider(new[]
            {
                new System.Numerics.Vector2(-160, -160),
                new System.Numerics.Vector2(160, -160),
                new System.Numerics.Vector2(160, 160),
                new System.Numerics.Vector2(-160, 160)
            });
            var ta = new Transform(new Vector2(860, 650), 1168.25146f);
            var translationA = Vector2.Zero;

            var b = new SphereCollider(0f);
            var tb = new Transform(new Vector2(675.546448f, 487.930237f), 0f);
            var translationB = new Vector2(67.5546494f, 48.7930183f);

            var result = Collision.Continuous(a, ta, translationA, b, tb, translationB, out var t, out var n);
            Assert.True(result);
            Assert.True(t is > 0f and < 1f);
            Assert.True(n.LengthSquared() > 0);
        }
    }
}
