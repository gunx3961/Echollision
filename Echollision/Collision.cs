using System;
using System.Diagnostics;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public class Collision
    {
        public Collision(
            float toleranceGjk = 1e-6f, // [van der Bergen 2003] P.143
            float relativeErrorBound = 0.001f, // 0.1%
            float toleranceMpr = 0.01f // This is just enough
        )
        {
            _toleranceGjk = toleranceGjk;
            _relativeErrorBound = relativeErrorBound;
            _toleranceMpr = toleranceMpr;
        }

        private readonly float _toleranceGjk;
        private readonly float _relativeErrorBound;
        private readonly float _toleranceMpr;

#if DEBUG
        internal readonly CollisionDetail Detail = new CollisionDetail();
#endif

        /// <summary>
        /// Distance query.
        /// </summary>
        /// <param name="a">Object A.</param>
        /// <param name="transformA">The transform of object A.</param>
        /// <param name="b">Object B.</param>
        /// <param name="transformB">The transform of object B.</param>
        /// <returns>The distance between two objects.</returns>
        public float Distance(
            Collider a, in ColliderTransform transformA,
            Collider b, in ColliderTransform transformB
        )
        {
            // Distance query by GJK distance algorithm with Signed Volumes distance sub-algorithm
            var k = 0;

#if DEBUG
            Detail.Clear();
#endif

            // Pick arbitrary support point as initial v
            var v = a.WorldSupport(transformA, Vector2.UnitX) - b.WorldSupport(transformB, -Vector2.UnitX);

            Span<Vector2> setW = stackalloc Vector2[3];
            var wCount = 0;
            Span<Vector2> setY = stackalloc Vector2[3];
            var yCount = 0;
            Span<float> lambda = stackalloc float[3];

            while (k < 64)
            {
                k += 1;
                var w = a.WorldSupport(transformA, -v) - b.WorldSupport(transformB, v);

#if DEBUG
                Detail.UpdateIterationCounter(k);
                // var negativeVDirection = Vector2.Normalize(-v) * 100;
                // DebugDraw.UpdateIterationCounter(k);
                // DebugDraw.DrawLine(Vector2.Zero, negativeVDirection);
                // DebugDraw.DrawString($"-v{(k - 1).ToString()}", negativeVDirection);
                // DebugDraw.DrawPoint(w);
                // DebugDraw.DrawString($"w{(k - 1).ToString()}", w);
#endif

                int i;
                for (i = 0; i < yCount; i += 1)
                {
                    if (w == setY[i]) return v.Length();
                }

                var vkLengthSquared = v.LengthSquared();
                var vDotW = Vector2.Dot(v, w);
                var vIsCloseToVFactor = _relativeErrorBound * vkLengthSquared;
                if (vkLengthSquared - vDotW <= vIsCloseToVFactor)
                {
                    return v.Length();
                }

                setW[wCount] = w;
                wCount += 1;
                for (i = 0; i < wCount; i += 1)
                {
                    setY[i] = setW[i];
                }

                yCount = wCount;

                DistanceSv(ref setW, ref lambda, ref wCount);
                v = Vector2.Zero;
                for (i = 0; i < wCount; i += 1)
                {
                    v += lambda[i] * setW[i];
                }

#if DEBUG
                Detail.PushGjkProcedure(wCount, setW, v, w);
                // DebugDraw.DrawGjkProcedure(wCount, setW, v, w);
                // DebugDraw.DrawPoint(v);
                // DebugDraw.DrawString($"v{k}", v);
#endif

                // Termination
                var maxWLengthSquared = setW[0].LengthSquared();
                for (i = 1; i < wCount; i += 1)
                {
                    var wls = setW[i].LengthSquared();
                    if (wls > maxWLengthSquared) maxWLengthSquared = wls;
                }

                if (wCount >= 3 || vkLengthSquared <= _toleranceGjk * maxWLengthSquared)
                {
                    // We regard v as zero
                    return 0f;
                }

                if (v == Vector2.Zero) return 0f;
            }

            return v.Length();
        }

        /// <summary>
        /// Continuous collision detection.<br/>
        /// </summary>
        /// <param name="a">Object A.</param>
        /// <param name="transformA">The transform of object A.</param>
        /// <param name="translationA">The movement will be applied to object A.</param>
        /// <param name="b">Object B.</param>
        /// <param name="transformB">The transform of object B.</param>
        /// <param name="translationB">The movement will be applied to object B.</param>
        /// <param name="toi">Time of impact.</param>
        /// <param name="normal">Normal at hit point from B to A, of which length is not guaranteed to be 1.</param>
        /// <returns>Whether will collide.</returns>
        public bool Continuous(
            Collider a, in ColliderTransform transformA, Vector2 translationA,
            Collider b, in ColliderTransform transformB, Vector2 translationB,
            out float toi, out Vector2 normal
        )
        {
            // Continuous a.k.a. priori collision detection by GJK Ray Cast

            var ray = translationB - translationA;
            toi = 0f; // Hit parameter a.k.a lambda a.k.a. time
            var x = Vector2.Zero; // Source is the origin
            normal = Vector2.Zero;

            // Initial v = x − “arbitrary point in C”
            var v = b.WorldSupport(transformB, -Vector2.UnitX) - a.WorldSupport(transformA, Vector2.UnitX);

#if DEBUG
            Detail.Clear();
            Detail.GjkRayCastContext.Ray = ray;
#endif

            Span<Vector2> setP = stackalloc Vector2[3];
            Span<Vector2> xMinusY = stackalloc Vector2[3];
            // To retrieve P from {x} − Y, we cannot just let p = x - (x - p).
            // Rarely there are cases that x - (x - p) != p because of floating point number precision.
            // Here I use the simple lookup solution.
            Span<(Vector2 xMinusP, Vector2 p)> lookup = stackalloc (Vector2, Vector2)[3];
            var pCount = 0;
            Span<float> lambda = stackalloc float[3];
            var k = 0;
            while (k < 64)
            {
                k += 1;
                int i;

                // Termination
                var vLengthSquared = v.LengthSquared();
                var maxPxLengthSquared = 0f;
                for (i = 0; i < pCount; i += 1)
                {
                    var ls = (x - setP[i]).LengthSquared();
                    if (ls > maxPxLengthSquared) maxPxLengthSquared = ls;
                }

                if (vLengthSquared <= _toleranceGjk * maxPxLengthSquared) break;

                var p = a.WorldSupport(transformA, v) - b.WorldSupport(transformB, -v);
                var w = x - p;

#if DEBUG
                Detail.PushGjkRayCastProcedure(x, p, pCount, setP, v);
#endif

                var vDotW = Vector2.Dot(v, w);
                if (vDotW > 0f)
                {
                    var vDotR = Vector2.Dot(v, ray);
                    if (vDotR >= 0f) return false;
                    toi = toi - vDotW / vDotR;
                    // Of course
                    if (toi > 1f) return false;
                    x = toi * ray;
                    normal = -v;
                }

                // Be careful to compute v(conv({x} − Y))
                for (i = 0; i < pCount; i += 1)
                {
                    xMinusY[i] = x - setP[i];
                    lookup[i].xMinusP = xMinusY[i];
                    lookup[i].p = setP[i];
                }

                var isNewP = true;
                for (i = 0; i < pCount; i += 1)
                {
                    if (p != setP[i]) continue;
                    isNewP = false;
                    break;
                }

                if (isNewP)
                {
                    xMinusY[pCount] = x - p;
                    lookup[pCount].xMinusP = xMinusY[pCount];
                    lookup[pCount].p = p;
                    pCount += 1;
                }

                var prevPCount = pCount;
                DistanceSv(ref xMinusY, ref lambda, ref pCount);
                v = Vector2.Zero;
                for (i = 0; i < pCount; i += 1)
                {
                    v += lambda[i] * xMinusY[i];
                    // retrieve P from {x} − Y
                    for (var j = 0; j < prevPCount; j += 1)
                    {
                        if (xMinusY[i] != lookup[j].xMinusP) continue;
                        setP[i] = lookup[j].p;
                        break;
                    }
                }
            }
#if DEBUG
            Detail.UpdateIterationCounter(k);
#endif

            return true;
        }

        /// <summary>
        /// Intersection detection.
        /// </summary>
        /// <param name="a">Object A.</param>
        /// <param name="transformA">The transform of object A.</param>
        /// <param name="b">Object B.</param>
        /// <param name="transformB">The transform of object B.</param>
        /// <returns>Whether two objects intersect.</returns>
        public bool Intersection(
            Collider a, in ColliderTransform transformA,
            Collider b, in ColliderTransform transformB
        )
        {
#if DEBUG
            Detail.Clear();
#endif

            var centerA = a.WorldCenter(transformA);
            var centerB = b.WorldCenter(transformB);
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) return true;

            // Origin ray is always the outer direction
            var originRay = -v0;
            var v1 = b.WorldSupport(transformB, originRay) - a.WorldSupport(transformA, -originRay);
            var v0v1 = v1 - v0;

            var supportDirection = v0v1;
            ToPositiveNormal(ref supportDirection);
            // If normal sign is positive then origin is at positive side of v0-v1
            var v0v1NormalSign = Math.Sign(Vector2.Dot(supportDirection, originRay));

            // Origin is at v0-v1 direction
            if (v0v1NormalSign == 0) return originRay.LengthSquared() <= v0v1.LengthSquared();

            supportDirection *= v0v1NormalSign;
            var v2 = b.WorldSupport(transformB, supportDirection) - a.WorldSupport(transformA, -supportDirection);

            // TODO: Safe option
            const int maxRefinement = 10;
            var i = 0;
            while (i++ < maxRefinement)
            {
                supportDirection = (v1 - v2) * v0v1NormalSign; // Make positive normal always point to outer direction 
                ToPositiveNormal(ref supportDirection);

                // Origin is inside the portal
                if (Vector2.Dot(supportDirection, v1) >= 0f) return true;

                var v3 = b.WorldSupport(transformB, supportDirection) - a.WorldSupport(transformA, -supportDirection);

#if DEBUG
                Detail.UpdateIterationCounter(i);
                Detail.PushMprProcedure(v0, v1, v2, v3);
#endif

                // Origin is outside of the support plane
                if (Vector2.Dot(supportDirection, v3) < 0f) return false;

                // Termination {support plane is closed enough to portal}
                // From what I have observed, MPR works very well even without this termination by tolerance.
                // Max refinement count limitation performs just like a relative error bound.
                // TODO: termination strategy
                var portalToSupportPlane = Vector2.Dot(Vector2.Normalize(supportDirection), v3 - v1);
                if (portalToSupportPlane <= _toleranceMpr) return false;

                supportDirection = v3 - v0;
                ToPositiveNormal(ref supportDirection);
                var v0v3NormalSign = Math.Sign(Vector2.Dot(supportDirection, originRay));

                // Origin is at v0-v3 direction
                // TODO: is this possible?
                if (v0v3NormalSign == 0) return originRay.LengthSquared() <= (v0 - v3).LengthSquared();

                // Choose new portal
                // Determine new v1 and v2 by two normal signs
                if (v0v1NormalSign * v0v3NormalSign == 1) v1 = v3;
                else v2 = v3;
            }

            return false;
        }

        /// <summary>
        /// Resolve penetration depth and contact normal.
        /// </summary>
        /// <param name="a">Object A.</param>
        /// <param name="transformA">The transform of object A.</param>
        /// <param name="b">Object B.</param>
        /// <param name="transformB">The transform of object B.</param>
        /// <param name="normal">Contact normal from B to A, of which length is not guaranteed to be 1.</param>
        /// <param name="depth">Penetration depth.</param>
        public void Penetration(
            Collider a, in ColliderTransform transformA,
            Collider b, in ColliderTransform transformB,
            out Vector2 normal, out float depth
        )
        {
#if DEBUG
            Detail.Clear();
#endif
            var centerA = a.WorldCenter(transformA);
            var centerB = b.WorldCenter(transformB);
            var v0 = centerB - centerA;

            // We simply use origin ray as the direction for resolving contact.
            // If the centers of two objects overlap, use following strategy to resolve:
            // sample support point of B-A in several direction and just find the 
            // closest point as the contact point, the contactPoint-v0 as contact normal.
            // TODO: proper deep penetration resolving strategy.
            if (v0 == Vector2.Zero)
            {
                Span<Vector2> sampleDirections = stackalloc Vector2[]
                {
                    new Vector2(0f, -1f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(-1f, 0f)
                };

                var minSupport = Vector2.Zero;
                var minLengthSquared = float.MaxValue;
                for (var i = 0; i < sampleDirections.Length; i += 1)
                {
                    var support = b.WorldSupport(transformB, sampleDirections[i]) -
                                  a.WorldSupport(transformA, -sampleDirections[i]);
                    var lengthSquared = support.LengthSquared();
                    if (!(lengthSquared < minLengthSquared)) continue;

                    minLengthSquared = lengthSquared;
                    minSupport = support;
                }

                normal = minSupport;

#if NETSTANDARD2_0
                depth = (float) Math.Sqrt(Convert.ToDouble(minLengthSquared));
#else
                depth = MathF.Sqrt(minLengthSquared);
#endif

#if DEBUG
                var aPoint = a.WorldSupport(transformA, -normal);
                var bPoint = b.WorldSupport(transformB, normal);
                Detail.PenetrationContext.PointA = aPoint;
                Detail.PenetrationContext.PointB = bPoint;
                Detail.PenetrationContext.Normal = normal;
#endif
                return;
            }

            var originRay = -v0;
            var v1 = b.WorldSupport(transformB, originRay) - a.WorldSupport(transformA, -originRay);
            var v0v1 = v1 - v0;

            var supportDirection = v0v1;
            ToPositiveNormal(ref supportDirection);
            var v0v1NormalSign = Math.Sign(Vector2.Dot(supportDirection, originRay));

            // Origin is at v0-v1 direction
            if (v0v1NormalSign == 0)
            {
                depth = v0v1.Length() - originRay.Length();
                normal = originRay;
#if DEBUG
                var aPoint = a.WorldSupport(transformA, -supportDirection);
                var bPoint = b.WorldSupport(transformB, supportDirection);
                Detail.PenetrationContext.PointA = aPoint;
                Detail.PenetrationContext.PointB = bPoint;
                Detail.PenetrationContext.Normal = normal;
#endif
                return;
            }

            supportDirection *= v0v1NormalSign;
            var v2 = b.WorldSupport(transformB, supportDirection) - a.WorldSupport(transformA, -supportDirection);

            // Do refinement until portal reaches the boundary
            const int maxRefinement = 256;
            var k = 0;
            while (k++ < maxRefinement)
            {
                supportDirection = (v1 - v2) * v0v1NormalSign;
                ToPositiveNormal(ref supportDirection);

                var v3 = b.WorldSupport(transformB, supportDirection) - a.WorldSupport(transformA, -supportDirection);

#if DEBUG
                Detail.UpdateIterationCounter(k);
                Detail.PushMprProcedure(v0, v1, v2, v3);
#endif
                // Portal reaches the boundary
                var portalToSupportPlane = Vector2.Dot(Vector2.Normalize(supportDirection), v3 - v1);
                if (portalToSupportPlane <= _toleranceMpr)
                {
                    normal = Vector2.Normalize(supportDirection);
                    depth = Vector2.Dot(v3, normal);
#if DEBUG
                    var aPoint = a.WorldSupport(transformA, -supportDirection);
                    var bPoint = b.WorldSupport(transformB, supportDirection);
                    Detail.PenetrationContext.PointA = aPoint;
                    Detail.PenetrationContext.PointB = bPoint;
                    Detail.PenetrationContext.Normal = normal;
#endif
                    return;
                }

                supportDirection = v3 - v0;
                ToPositiveNormal(ref supportDirection);
                var v0v3NormalSign = Math.Sign(Vector2.Dot(supportDirection, originRay));

                // Origin is at v0-v3 direction
                if (v0v3NormalSign == 0)
                {
                    depth = (v0 - v3).Length() - originRay.Length();
                    normal = originRay;
#if DEBUG
                    var aPoint = a.WorldSupport(transformA, -supportDirection);
                    var bPoint = b.WorldSupport(transformB, supportDirection);
                    Detail.PenetrationContext.PointA = aPoint;
                    Detail.PenetrationContext.PointB = bPoint;
                    Detail.PenetrationContext.Normal = normal;
#endif
                    return;
                }

                // Choose new portal
                // Determine new v1 and v2 by two normal signs
                if (v0v1NormalSign * v0v3NormalSign == 1) v1 = v3;
                else v2 = v3;
            }

            // Cannot be here
#if DEBUG
            throw new ApplicationException("Unexpected error.");
#endif
            normal = Vector2.Zero;
            depth = 0f;
        }

        #region MPR Utils

        // Here we define that vector(-y, x) is the POSITIVE normal of vector(x, y)
        private static void ToPositiveNormal(ref Vector2 vector)
        {
            vector = new Vector2(-vector.Y, vector.X);
        }

        #endregion

        #region Distance Sub-algorithm

        // Signed-volume method
        private static void DistanceSv(ref Span<Vector2> tau, ref Span<float> lambda, ref int vertexCount)
        {
            switch (vertexCount)
            {
                case 3:
                    S2D(ref tau, ref lambda, ref vertexCount);
                    break;
                case 2:
                    S1D(ref tau, ref lambda, ref vertexCount);
                    break;
                case 1:
                    lambda[0] = 1f;
                    vertexCount = 1;
                    break;
                default:
                    Debugger.Break();
                    throw new ApplicationException("Assertion Error");
            }
        }

        private static bool IsSameSign(float a, float b)
        {
            return (a > 0f && b > 0f) || (a < 0f && b < 0f);
        }

        private static void S2D(ref Span<Vector2> w, ref Span<float> lambda, ref int vertexCount)
        {
            var s1 = w[0];
            var s2 = w[1];
            var s3 = w[2];

            var cofactor31 = (s2.X * s3.Y) - (s3.X * s2.Y);
            var cofactor32 = (s3.X * s1.Y) - (s1.X * s3.Y);
            var cofactor33 = (s1.X * s2.Y) - (s2.X * s1.Y);
            var detM = cofactor31 + cofactor32 + cofactor33;

            var isSame1 = IsSameSign(detM, cofactor31);
            var isSame2 = IsSameSign(detM, cofactor32);
            var isSame3 = IsSameSign(detM, cofactor33);

            if (isSame1 && isSame2 && isSame3)
            {
                // Origin is inside the 2-simplex 
                lambda[0] = cofactor31 / detM;
                lambda[1] = cofactor32 / detM;
                lambda[2] = cofactor33 / detM;
                vertexCount = 3;
                return;
            }

            Span<Vector2> tempW = stackalloc Vector2[2];
            Span<float> tempLambda = stackalloc float[2];
            var tempVertexCount = 2;
            int i;
            Vector2 tempV;
            float tempLengthSquared;
            var minDistanceSquared = float.MaxValue;

            if (!isSame1)
            {
                tempW[0] = s2;
                tempW[1] = s3;
                S1D(ref tempW, ref tempLambda, ref tempVertexCount);
                tempV = Vector2.Zero;
                for (i = 0; i < tempVertexCount; i += 1)
                {
                    tempV += tempLambda[i] * tempW[i];
                }

                tempLengthSquared = tempV.LengthSquared();
                if (tempLengthSquared < minDistanceSquared)
                {
                    minDistanceSquared = tempLengthSquared;
                    vertexCount = tempVertexCount;
                    for (i = 0; i < tempVertexCount; i += 1)
                    {
                        w[i] = tempW[i];
                        lambda[i] = tempLambda[i];
                    }
                }
            }

            if (!isSame2)
            {
                tempW[0] = s1;
                tempW[1] = s3;
                S1D(ref tempW, ref tempLambda, ref tempVertexCount);
                tempV = Vector2.Zero;
                for (i = 0; i < tempVertexCount; i += 1)
                {
                    tempV += tempLambda[i] * tempW[i];
                }

                tempLengthSquared = tempV.LengthSquared();
                if (tempLengthSquared < minDistanceSquared)
                {
                    minDistanceSquared = tempLengthSquared;
                    vertexCount = tempVertexCount;
                    for (i = 0; i < tempVertexCount; i += 1)
                    {
                        w[i] = tempW[i];
                        lambda[i] = tempLambda[i];
                    }
                }
            }

            if (!isSame3)
            {
                tempW[0] = s1;
                tempW[1] = s2;
                S1D(ref tempW, ref tempLambda, ref tempVertexCount);
                tempV = Vector2.Zero;
                for (i = 0; i < tempVertexCount; i += 1)
                {
                    tempV += tempLambda[i] * tempW[i];
                }

                if (tempV.LengthSquared() < minDistanceSquared)
                {
                    vertexCount = tempVertexCount;
                    for (i = 0; i < tempVertexCount; i += 1)
                    {
                        w[i] = tempW[i];
                        lambda[i] = tempLambda[i];
                    }
                }
            }
        }

        private static void S1D(ref Span<Vector2> w, ref Span<float> lambda, ref int vertexCount)
        {
            var s1 = w[0];
            var s2 = w[1];

            var t = s2 - s1;
            var po = Vector2.Dot(-s1, t) / t.LengthSquared() * t + s1;


            Span<float> s1Components = stackalloc float[] {s1.X, s1.Y};
            Span<float> s2Components = stackalloc float[] {s2.X, s2.Y};
            Span<float> poComponents = stackalloc float[] {po.X, po.Y};
            var s2S1 = s1 - s2;
            var componentIndex = 0;
            var miuMax = s2S1.X;
            if (Math.Abs(s2S1.Y) > Math.Abs(s2S1.X))
            {
                componentIndex = 1;
                miuMax = s2S1.Y;
            }

            var cofactor1 = poComponents[componentIndex] - s2Components[componentIndex];
            var cofactor2 = s1Components[componentIndex] - poComponents[componentIndex];

            // ----*-----*-----*----
            //     po    s2    s1
            if (!IsSameSign(miuMax, cofactor1))
            {
                w[0] = s2;
                lambda[0] = 1f;
                vertexCount = 1;
                return;
            }

            // ----*-----*-----*----
            //     po    s1    s2
            if (!IsSameSign(miuMax, cofactor2))
            {
                // w[0] = s1;
                lambda[0] = 1f;
                vertexCount = 1;
                return;
            }

            // ----*-----*-----*----
            //     s1    po    s2
            lambda[0] = cofactor1 / miuMax;
            lambda[1] = cofactor2 / miuMax;
            vertexCount = 2;
        }

        #endregion
    }
}
