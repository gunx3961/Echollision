using System;
using System.Collections.Generic;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using ViLAWAVE.Echollision;

namespace Benchmark
{
    public class TestBenchmark
    {
        private const int N = 10;
        private const float Min = -1000f;
        private const float Max = 1000f;
        private readonly Random _rs = new Random();

        [ParamsSource(nameof(RandomSphereSweptArea))]
        public SphereSweptArea A;
        [ParamsSource(nameof(RandomSphereSweptArea))]
        public SphereSweptArea B;
        
        public IEnumerable<SphereSweptArea> RandomSphereSweptArea()
        {
            for (var i = 0; i < N; i += 1)
            {
                var range = Max - Min;
                var a = new Vector2(Min + (float) _rs.NextDouble() * range, Min + (float) _rs.NextDouble() * range);
                var b = new Vector2(Min + (float) _rs.NextDouble() * range, Min + (float) _rs.NextDouble() * range);
                var radius = Min + (float) _rs.NextDouble() * range;

                yield return new SphereSweptArea(a, b, radius);
            }
        }

        [Benchmark]
        public bool Intersection()
        {
            return BroadPhase.Intersection(ref A, ref B);
        }
    }
}