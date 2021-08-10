using System;
using System.Diagnostics;
using Xunit;
using System.Numerics;
using ViLAWAVE.Echollision;

namespace Test
{
    public class NarrowPhase
    {
        private readonly Collision _collision = new Collision();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Random _rs = new Random();
        private float RandomFloat(float min, float range) => min + (float) _rs.NextDouble() * range;

        [Fact]
        public void ContinuousIllCase()
        {
            var a = new ConvexCollider(new[]
            {
                new Vector2(-160, -160),
                new Vector2(160, -160),
                new Vector2(160, 160),
                new Vector2(-160, 160)
            });
            var ta = new ColliderTransform(new Vector2(860, 650), 1168.25146f);
            var translationA = Vector2.Zero;

            var b = new SphereCollider(0f);
            var tb = new ColliderTransform(new Vector2(675.546448f, 487.930237f), 0f);
            var translationB = new Vector2(67.5546494f, 48.7930183f);

            var result = _collision.Continuous(a, ta, translationA, b, tb, translationB, out var t, out var n);
            Assert.True(result);
            Assert.True(t is > 0f and < 1f);
            Assert.True(n.LengthSquared() > 0);
        }

        [Fact]
        public void PenetrationIllCase()
        {
            var a = new SphereCollider(0);
            var ta = new ColliderTransform(new Vector2(203.33334f, 239.99979f));

            var b = new ConvexCollider(new[]
            {
                new Vector2(-63.5f, -0.5f),
                new Vector2(64.5f, 0.5f),
                
            });
            var tb = new ColliderTransform(new Vector2(300, 300));

            _collision.Penetration(a, ta, b, tb, out var n, out var depth);
            var intersection = _collision.Intersection(a, ta, b, tb);
            Assert.False(intersection);
        }

        [Fact]
        public void NoIllCase()
        {
            float R() => RandomFloat(-1000f, 2000f);

            for (var i = 0; i < 1000; i += 1)
            {
                var randomA = new SphereCollider(R());
                var randomB = new SphereCollider(R());
                var rtA = new ColliderTransform(new Vector2(R(), R()), R());
                var rtB = new ColliderTransform(new Vector2(R(), R()), R());
                var rmA = new Vector2(R(), R());
                var rmB = new Vector2(R(), R());

                _stopwatch.Restart();
                _collision.Intersection(randomA, rtA, randomB, rtB);
                _stopwatch.Stop();
                if (_stopwatch.Elapsed.TotalMilliseconds > 0.5) Debugger.Break();
                Debug.WriteLine($"Intersection: {_stopwatch.Elapsed.TotalMilliseconds} ms");


                _stopwatch.Restart();
                _collision.Distance(randomA, rtA, randomB, rtB);
                _stopwatch.Stop();
                if (_stopwatch.Elapsed.TotalMilliseconds > 0.5) Debugger.Break();
                Debug.WriteLine($"Distance: {_stopwatch.Elapsed.TotalMilliseconds} ms");


                _stopwatch.Restart();
                _collision.Penetration(randomA, rtA, randomB, rtB, out var n, out var d);
                _stopwatch.Stop();
                if (_stopwatch.Elapsed.TotalMilliseconds > 0.5) Debugger.Break();
                Debug.WriteLine($"PenetrationDepth: {_stopwatch.Elapsed.TotalMilliseconds} ms");


                _stopwatch.Restart();
                _collision.Continuous(randomA, rtA, rmA, randomB, rtB, rmB, out var t, out n);
                _stopwatch.Stop();
                if (_stopwatch.Elapsed.TotalMilliseconds > 0.5) Debugger.Break();
                Debug.WriteLine($"Continuous: {_stopwatch.Elapsed.TotalMilliseconds} ms");
            }
        }

        [Fact]
        public void IntersectionCorrectness()
        {
        }

        [Fact]
        public void PrioriCorrectness()
        {
            var a = new SphereCollider(0f);
            var transformA = new ColliderTransform(Vector2.Zero, 0f);
            var translationA = new Vector2(19f, 0f);

            var b = new SegmentCollider(new Vector2(0f, -10f), new Vector2(0f, 10f));
            var transformB = new ColliderTransform(new Vector2(10f, 0f), 0f);
            var translationB = Vector2.Zero;

            var result = _collision.Continuous(a, transformA, translationA, b, transformB, translationB, out var t, out var n);
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
