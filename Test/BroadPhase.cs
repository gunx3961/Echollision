using System.Numerics;
using Xunit;
using ViLAWAVE.Echollision;

namespace Test
{
    public class BroadPhase
    {
        [Fact]
        public void RandomCase()
        {
            var a = new SphereSweptArea(Vector2.One, Vector2.Zero, 1f);
            var b = new SphereSweptArea(a: Vector2.One, Vector2.Zero, 1f);
            var result = ViLAWAVE.Echollision.BroadPhase.Intersection(ref a, ref b);
            
            Assert.True(result);
        }
    }
}