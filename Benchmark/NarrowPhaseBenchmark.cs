using System;
using System.Collections.Generic;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using ViLAWAVE.Echollision;

namespace Benchmark
{
    public class NarrowPhaseBenchmark
    {
        private const float Min = -1000f;
        private const float Max = 1000f;
        private readonly Collision _collision = new Collision();
        private readonly Random _rs = new Random();

        [ParamsSource(nameof(RandomAllTypeColliders))]
        public Collider ColliderA;

        [ParamsSource(nameof(RandomAllTypeColliders))]
        public Collider ColliderB;

        [ParamsSource(nameof(RandomTransform))]
        public (Vector2, float) TransformA;

        [ParamsSource(nameof(RandomTransform))]
        public (Vector2, float) TransformB;

        [ParamsSource(nameof(RandomMovement))] public Vector2 MovementA;
        [ParamsSource(nameof(RandomMovement))] public Vector2 MovementB;

        public IEnumerable<Collider> RandomAllTypeColliders()
        {
            const float range = Max - Min;
            float RandomFloat() => Min + (float) _rs.NextDouble() * range;

            yield return new SphereCollider(RandomFloat());
        }

        public IEnumerable<(Vector2 position, float rotation)> RandomTransform()
        {
            const float range = Max - Min;
            float RandomFloat() => Min + (float) _rs.NextDouble() * range;

            yield return (new Vector2(RandomFloat(), RandomFloat()), RandomFloat());
        }

        public IEnumerable<Vector2> RandomMovement()
        {
            const float range = Max - Min;
            float RandomFloat() => Min + (float) _rs.NextDouble() * range;
            yield return new Vector2(RandomFloat(), RandomFloat());
        }

        [Benchmark]
        public bool Intersection()
        {
            var ta = new ColliderTransform(TransformA.Item1, TransformA.Item2);
            var tb = new ColliderTransform(TransformB.Item1, TransformB.Item2);
            return _collision.Intersection(ColliderA, ta, ColliderB, tb);
        }

        [Benchmark]
        public float Distance()
        {
            var ta = new ColliderTransform(TransformA.Item1, TransformA.Item2);
            var tb = new ColliderTransform(TransformB.Item1, TransformB.Item2);
            return _collision.Distance(ColliderA, ta, ColliderB, tb);
        }

        [Benchmark]
        public void PenetrationDepth()
        {
            var ta = new ColliderTransform(TransformA.Item1, TransformA.Item2);
            var tb = new ColliderTransform(TransformB.Item1, TransformB.Item2);
            _collision.Penetration(ColliderA, ta, ColliderB, tb, out var n, out var d);
            return;
        }

        [Benchmark]
        public bool Continuous()
        {
            var ta = new ColliderTransform(TransformA.Item1, TransformA.Item2);
            var tb = new ColliderTransform(TransformB.Item1, TransformB.Item2);
            return _collision.Continuous(ColliderA, ta, MovementA, ColliderB, tb, MovementB, out var t, out var n);
        }
    }
}