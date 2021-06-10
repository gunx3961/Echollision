using System;
using Xunit;
using System.Numerics;
using ViLAWAVE.Echollision;

namespace Test
{
    public class NarrowPhase
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

        [Fact]
        public void IntersectionCorrectness()
        {
            
        }

        [Fact]
        public void PrioriCorrectness()
        {
            var a = new SphereCollider(0f);
            var transformA = new Transform(Vector2.Zero, 0f);
            var translationA = new Vector2(19f, 0f);
            
            var b = new SegmentCollider(new Vector2(0f, -10f), new Vector2(0f, 10f));
            var transformB = new Transform(new Vector2(10f, 0f), 0f);
            var translationB = Vector2.Zero;
            
            var result = Collision.Continuous(a, transformA, translationA, b, transformB, translationB, out var t, out var n);
            Assert.True(result);
            var exceptedT = 10f / 19f;
            Assert.True(t < exceptedT + 0.1f && t > exceptedT - 0.1f);
        }

        [Fact]
        public void PenetrationDepthCorrectness()
        {
            
        }

        [Fact]
        public void CenteredOriginPenetrationDepth()
        {
            
        }
    }
}
