﻿#define DEBUG_DRAW

using System;
using System.Diagnostics;
using System.Numerics;
using ViLAWAVE.Echollision.Collider;

namespace ViLAWAVE.Echollision
{
    public static class Collision
    {
        // TODO: configuration
        private const float ToleranceGjk = 1e-6f; // [van der Bergen 2003] P.143
        private const float RelativeErrorBound = 0.001f; // 0.1%
        
        private const float ToleranceMpr = 0.01f; // This is just enough

#if DEBUG_DRAW
        // TODO: Debug info
        public static int Foobar = 1;
#endif

        /// <summary>
        /// Distance query.
        /// </summary>
        /// <param name="a">Object A.</param>
        /// <param name="transformA">The transform of object A.</param>
        /// <param name="b">Object B.</param>
        /// <param name="transformB">The transform of object B.</param>
        /// <returns>The distance between two objects.</returns>
        public static float Distance(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB
        )
        {
            // Distance query via GJK distance algorithm with Signed Volumes distance sub-algorithm
            var k = 0;

#if DEBUG_DRAW
            DebugDraw.Clear();
            DebugDraw.DrawString("origin", Vector2.Zero);
            DebugDraw.DrawPoint(Vector2.Zero);
#endif

            // Pick arbitrary support point as initial v
            var v = a.WorldSupport(transformA, Vector2.UnitX) - b.WorldSupport(transformB, -Vector2.UnitX);

            Span<Vector2> setW = stackalloc Vector2[3];
            var wCount = 0;
            Span<Vector2> setY = stackalloc Vector2[3];
            var yCount = 0;
            Span<float> lambda = stackalloc float[3];

            while (k < 65535)
            {
                k += 1;
                var w = a.WorldSupport(transformA, -v) - b.WorldSupport(transformB, v);

#if DEBUG_DRAW
                var negativeVDirection = Vector2.Normalize(-v) * 100;
                DebugDraw.UpdateIterationCounter(k);
                DebugDraw.DrawLine(Vector2.Zero, negativeVDirection);
                DebugDraw.DrawString($"-v{(k - 1).ToString()}", negativeVDirection);
                DebugDraw.DrawPoint(w);
                DebugDraw.DrawString($"w{(k - 1).ToString()}", w);
#endif

                int i;
                for (i = 0; i < yCount; i += 1)
                {
                    if (w == setY[i]) return v.Length();
                }

                var vkLengthSquared = v.LengthSquared();
                var vDotW = Vector2.Dot(v, w);
                var vIsCloseToVFactor = RelativeErrorBound * vkLengthSquared;
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

#if DEBUG_DRAW
                DebugDraw.DrawGjkProcedure(wCount, setW, v, w);
                DebugDraw.DrawPoint(v);
                DebugDraw.DrawString($"v{k}", v);
#endif

                // Termination
                var maxWLengthSquared = setW[0].LengthSquared();
                for (i = 1; i < wCount; i += 1)
                {
                    var wls = setW[i].LengthSquared();
                    if (wls > maxWLengthSquared) maxWLengthSquared = wls;
                }

                if (wCount >= 3 || vkLengthSquared <= ToleranceGjk * maxWLengthSquared)
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
        /// <param name="t">Hit parameter a.k.a. time.</param>
        /// <param name="normal">Normal at hit point, of which length is not guaranteed to be 1.</param>
        /// <returns>Whether will collide.</returns>
        public static bool Continuous(
            ICollider a, in Transform transformA, Vector2 translationA,
            ICollider b, in Transform transformB, Vector2 translationB,
            out float t, out Vector2 normal
        )
        {
            // Continuous a.k.a. priori collision detection via GJK Ray Cast

            var ray = translationB - translationA;
            t = 0f; // Hit parameter a.k.a lambda a.k.a. time
            var x = Vector2.Zero; // Source is the origin
            normal = Vector2.Zero;

            // Initial v = x − “arbitrary point in C”
            var v = -(a.WorldSupport(transformA, Vector2.UnitX) - b.WorldSupport(transformB, -Vector2.UnitX));

#if DEBUG_DRAW
            DebugDraw.Clear();
            DebugDraw.DrawString("origin", Vector2.Zero);
            DebugDraw.DrawPoint(Vector2.Zero);
            DebugDraw.DrawString("ray", ray);
            DebugDraw.DrawLine(Vector2.Zero, ray);
#endif

            Span<Vector2> setP = stackalloc Vector2[3];
            Span<Vector2> xMinusY = stackalloc Vector2[3];
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

                if (vLengthSquared <= ToleranceGjk * maxPxLengthSquared) break;

                var p = a.WorldSupport(transformA, v) - b.WorldSupport(transformB, -v);
                var w = x - p;

#if DEBUG_DRAW
                DebugDraw.DrawGjkRayCastProcedure(x, p, pCount, setP, v);
#endif

                var vDotW = Vector2.Dot(v, w);
                if (vDotW > 0f)
                {
                    var vDotR = Vector2.Dot(v, ray);
                    if (vDotR >= 0f) return false;
                    t = t - vDotW / vDotR;
                    // Of course
                    if (t > 1f) return false;
                    x = t * ray;
                    normal = v;
                }

                // Be careful to compute v(conv({x} − Y))
                for (i = 0; i < pCount; i += 1)
                {
                    xMinusY[i] = x - setP[i];
                    lookup[i].p = setP[i];
                    lookup[i].xMinusP = xMinusY[i];
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
                    lookup[pCount].p = p;
                    lookup[pCount].xMinusP = xMinusY[i];
                    pCount += 1;
                }

                DistanceSv(ref xMinusY, ref lambda, ref pCount);
                v = Vector2.Zero;
                for (i = 0; i < pCount; i += 1)
                {
                    v += lambda[i] * xMinusY[i];
                    // get P from {x} − Y
                    for (var j = 0; j < pCount; j += 1)
                    {
                        if (xMinusY[i] != lookup[j].xMinusP) continue;
                        setP[i] = lookup[j].p;
                    }
                }
            }
#if DEBUG_DRAW
            DebugDraw.UpdateIterationCounter(k);
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
        public static bool Intersection(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB
        )
        {
            // Intersection detection via MPR 

            var centerA = a.WorldCenter(transformA);
            var centerB = b.WorldCenter(transformB);
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) v0 = new Vector2(0.00001f, 0);

            var normal = Vector2.Normalize(-v0);

            var v1 = b.WorldSupport(transformB, normal) - a.WorldSupport(transformA, -normal);

            normal = Vector2.Normalize(v1 - v0);
            normal = new Vector2(normal.Y, -normal.X);
            if (Vector2.Dot(-v0, normal) < 0) normal = -normal;

            var v2 = b.WorldSupport(transformB, normal) - a.WorldSupport(transformA, -normal);

#if DEBUG_DRAW
            DebugDraw.Clear();
            DebugDraw.DrawString("origin", Vector2.Zero);
            DebugDraw.DrawPoint(Vector2.Zero);
            DebugDraw.DrawString("v0", v0);
            DebugDraw.DrawPoint(v0);
            DebugDraw.DrawString("origin ray", v0 + normal * 240);
            DebugDraw.DrawLine(v0, v0 + normal * 240);
            DebugDraw.DrawString("v1", v1);
            DebugDraw.DrawPoint(v1);
            DebugDraw.DrawLine(v0, v1);
            DebugDraw.DrawString("v2", v2);
            DebugDraw.DrawPoint(v2);
            DebugDraw.DrawLine(v0, v2);
            DebugDraw.DrawLine(v1, v2);
#endif
            const int maxRefinement = 5;
            var counter = maxRefinement;
            while (counter > 0)
            {
                normal = Vector2.Normalize(v2 - v1);
                normal = new Vector2(normal.Y, -normal.X);
                if (Vector2.Dot(normal, v0 - v1) > 0) normal = -normal; // Outer normal

#if DEBUG_DRAW
                var debugMidPoint = (v1 + v2) / 2;
                DebugDraw.DrawLine(debugMidPoint, debugMidPoint + normal * 100);
                DebugDraw.DrawString("n", debugMidPoint + normal * 100);
#endif

                if (Vector2.Dot(normal, -v1) < 0) return true;

                var v3 = b.WorldSupport(transformB, normal) - a.WorldSupport(transformA, -normal);
#if DEBUG_DRAW
                DebugDraw.DrawLine(v0, v3);
#endif

                if (Vector2.Dot(normal, v3) < 0) return false;

                normal = Vector2.Normalize(v3 - v0);
                normal = new Vector2(normal.Y, -normal.X);

                if (Vector2.Dot(v2 - v1, normal) > 0 ^ Vector2.Dot(-v0, normal) > 0) // in v1 side
                {
                    v2 = v3;
#if DEBUG_DRAW
                    DebugDraw.DrawLine(v1, v3);
#endif
                }
                else
                {
                    v1 = v3;
#if DEBUG_DRAW
                    DebugDraw.DrawLine(v2, v3);
#endif
                }

                counter -= 1;
            }

            return false;
        }

        /// <summary>
        /// Intersection detection.
        /// </summary>
        /// <param name="a">Object A.</param>
        /// <param name="transformA">The transform of object A.</param>
        /// <param name="b">Object B.</param>
        /// <param name="transformB">The transform of object B.</param>
        /// <returns>Whether two objects intersect.</returns>
        public static bool IntersectionNew(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB
        )
        {
#if DEBUG_DRAW
            DebugDraw.Clear();
            DebugDraw.DrawString("origin", Vector2.Zero);
            DebugDraw.DrawPoint(Vector2.Zero);
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

#if DEBUG_DRAW
                DebugDraw.UpdateIterationCounter(i);
                DebugDraw.DrawMprProcedure(v0, v1, v2, v3);
#endif

                // Origin is outside of the support plane
                if (Vector2.Dot(supportDirection, v3) < 0f) return false;

                // Termination {support plane is closed enough to portal}
                // From what I have observed, MPR works very well even without this termination via tolerance.
                // Max refinement count limitation performs just like a relative error bound.
                // TODO: termination strategy
                var portalToSupportPlane = Vector2.Dot(Vector2.Normalize(supportDirection), v3 - v1);
                if (portalToSupportPlane <= ToleranceMpr) return false;

                supportDirection = v3 - v0;
                ToPositiveNormal(ref supportDirection);
                var v0v3NormalSign = Math.Sign(Vector2.Dot(supportDirection, originRay));

                // Origin is at v0-v3 direction
                // TODO: is this possible?
                if (v0v3NormalSign == 0) return originRay.LengthSquared() <= (v0 - v3).LengthSquared();

                // Choose new portal
                // Determine new v1 and v2 via two normal signs
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
        public static void PenetrationDepth(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB,
            out Vector2 normal, out float depth
        )
        {
#if DEBUG_DRAW
            DebugDraw.Clear();
            DebugDraw.DrawString("origin", Vector2.Zero);
            DebugDraw.DrawPoint(Vector2.Zero);
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
                depth = MathF.Sqrt(minLengthSquared);
#if DEBUG_DRAW
                var aPoint = a.WorldSupport(transformA, -normal);
                var bPoint = b.WorldSupport(transformB, normal);
                DebugDraw.DrawPenetration(aPoint, bPoint, normal);
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
#if DEBUG_DRAW
                var aPoint = a.WorldSupport(transformA, -supportDirection);
                var bPoint = b.WorldSupport(transformB, supportDirection);
                DebugDraw.DrawPenetration(aPoint, bPoint, normal);
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

#if DEBUG_DRAW
                DebugDraw.UpdateIterationCounter(k);
                DebugDraw.DrawMprProcedure(v0, v1, v2, v3);
#endif
                // Portal reaches the boundary
                var portalToSupportPlane = Vector2.Dot(Vector2.Normalize(supportDirection), v3 - v1);
                if (portalToSupportPlane <= ToleranceMpr)
                {
                    normal = Vector2.Normalize(supportDirection);
                    depth = Vector2.Dot(v3, normal);
#if DEBUG_DRAW
                    var aPoint = a.WorldSupport(transformA, -supportDirection);
                    var bPoint = b.WorldSupport(transformB, supportDirection);
                    DebugDraw.DrawPenetration(aPoint, bPoint, normal);
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
#if DEBUG_DRAW
                    var aPoint = a.WorldSupport(transformA, -supportDirection);
                    var bPoint = b.WorldSupport(transformB, supportDirection);
                    DebugDraw.DrawPenetration(aPoint, bPoint, normal);
#endif
                    return;
                }

                // Choose new portal
                // Determine new v1 and v2 via two normal signs
                if (v0v1NormalSign * v0v3NormalSign == 1) v1 = v3;
                else v2 = v3;
            }

            // Cannot be here
#if DEBUG_DRAW
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
            ref var s1 = ref w[0];
            ref var s2 = ref w[1];
            ref var s3 = ref w[2];

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
            ref var s1 = ref w[0];
            ref var s2 = ref w[1];

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

        public static Vector2 WorldSupport(this ICollider shape, in Transform transform, Vector2 normal)
        {
            var rotation = Matrix3x2.CreateRotation(transform.Rotation);
            Matrix3x2.Invert(rotation, out var inverted);
            var localNormal = Vector2.TransformNormal(normal, inverted);
            var supportLocal = shape.Support(localNormal);
            var supportWorld = Vector2.Transform(supportLocal, rotation) + transform.Translation;
            return supportWorld;
        }

        private static Vector2 WorldCenter(this ICollider shape, in Transform transform)
        {
            var rotation = Matrix3x2.CreateRotation(transform.Rotation);
            return Vector2.Transform(shape.Center, rotation) + transform.Translation;
        }

        #region EPA Utils

        // Priority queue implementation via binary heap.
        private ref struct PriorityQueue
        {
            internal PriorityQueue(Span<Entry> buffer)
            {
                _buffer = buffer;
                _i = -1;
            }

            private readonly Span<Entry> _buffer;
            private int _i;

            internal void Push(ref Entry e)
            {
                _i += 1;
                _buffer[_i] = e;

                var pos = _i;
                var thisKey = e.distance;
                while (pos > 0)
                {
                    var parent = Parent(pos);
                    if (_buffer[parent].distance > thisKey)
                    {
                        Swap(parent, pos);
                    }

                    pos = parent;
                }
            }

            internal Entry PopBest()
            {
                if (_i < 0) throw new ApplicationException("Cannot pop from empty queue.");

                var best = _buffer[0];
                _i -= 1;
                if (_i == -1) return best;

                // TODO: can make it lazy
                _buffer[0] = _buffer[_i + 1];
                var pos = 0;
                while (true)
                {
                    var smallest = pos;
                    var leftChild = LeftChild(smallest);
                    var rightChild = leftChild + 1;

                    if (leftChild <= _i && _buffer[leftChild].distance < _buffer[smallest].distance)
                        smallest = leftChild;
                    if (rightChild <= _i && _buffer[rightChild].distance < _buffer[smallest].distance)
                        smallest = rightChild;

                    if (smallest == pos) break;
                    Swap(pos, smallest);
                    pos = smallest;
                }

                return best;
            }

            private void Swap(int a, int b)
            {
                var temp = _buffer[a];
                _buffer[a] = _buffer[b];
                _buffer[b] = temp;
            }

            // TODO: make them inline
            private static int Parent(int i)
            {
                return (i - 1) / 2;
            }

            private static int LeftChild(int i)
            {
                return 2 * i + 1;
            }
        }

        private struct Entry
        {
            public Vector2 y1;
            public Vector2 y2;
            public Vector2 v;
            public float distance;
        }

        #endregion
    }
}