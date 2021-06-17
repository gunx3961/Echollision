using System.Numerics;
using ViLAWAVE.Echollision.BroadPhase;
using Xunit;

namespace Test
{
    public class BroadPhase
    {
        [Fact]
        public void SphereSweptAreaIntersectionCorrectness()
        {
            var a = new SphereSweptArea(new Vector2(3, 4), new Vector2(6, 8), 1.3f);
            var b = new SphereSweptArea(a: new Vector2(2, 3.9f), new Vector2(-2, -4), 0.2f);
            var result = SphereSweptArea.Intersection(ref a, ref b);
            Assert.True(result);
            result = SphereSweptArea.Intersection(ref b, ref a);
            Assert.True(result);
            a = new SphereSweptArea(new Vector2(3, 4), new Vector2(6, 8), 1.3f);
            b = new SphereSweptArea(a: new Vector2(1, 2f), new Vector2(-2, -4), 0.2f);
            result = SphereSweptArea.Intersection(ref a, ref b);
            Assert.False(result);
            result = SphereSweptArea.Intersection(ref b, ref a);
            Assert.False(result);

            a = new SphereSweptArea(new Vector2(3, 4), new Vector2(6, 8), 0.00000001f);
            b = new SphereSweptArea(new Vector2(2, 1), new Vector2(-3, -10), 4);
            result = SphereSweptArea.Intersection(ref a, ref b);
            Assert.True(result);
            result = SphereSweptArea.Intersection(ref b, ref a);
            Assert.True(result);
            
            a = new SphereSweptArea(new Vector2(-8, -8), new Vector2(1, 30), 0f);
            b = new SphereSweptArea(new Vector2(-10, 11), new Vector2(23, -10), 0f);
            result = SphereSweptArea.Intersection(ref a, ref b);
            Assert.True(result);
            result = SphereSweptArea.Intersection(ref b, ref a);
            Assert.True(result);
            a = new SphereSweptArea(new Vector2(-8, -8), new Vector2(23, -10), 0f);
            b = new SphereSweptArea(new Vector2(-10, 11), new Vector2(1, 30), 0f);
            result = SphereSweptArea.Intersection(ref a, ref b);
            Assert.False(result);
            result = SphereSweptArea.Intersection(ref b, ref a);
            Assert.False(result);
            
            a = new SphereSweptArea(new Vector2(-8, -8), new Vector2(-8, -8), 10f);
            b = new SphereSweptArea(new Vector2(-10, 11), new Vector2(-10, 11), 10f);
            result = SphereSweptArea.Intersection(ref a, ref b);
            Assert.True(result);
            result = SphereSweptArea.Intersection(ref b, ref a);
            Assert.True(result);
            a = new SphereSweptArea(new Vector2(-8, -8), new Vector2(-8, -8), 9f);
            b = new SphereSweptArea(new Vector2(-10, 11), new Vector2(-10, 11), 9f);
            result = SphereSweptArea.Intersection(ref a, ref b);
            Assert.False(result);
            result = SphereSweptArea.Intersection(ref b, ref a);
            Assert.False(result);
        }
    }
}