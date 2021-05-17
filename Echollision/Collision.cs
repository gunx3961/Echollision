#define DEBUG_DRAW

using System;
using System.Diagnostics;
using System.Numerics;
using ViLAWAVE.Echollision.Collider;

namespace ViLAWAVE.Echollision
{
    public static class Collision
    {
        private const float Tolerance = 1e-6f; // [van der Bergen 2003] P.143
        private const float RelativeErrorBound = 0.001f; // 0.1%

        /// <summary>
        /// Distance query via GJK
        /// </summary>
        /// <param name="a"></param>
        /// <param name="transformA"></param>
        /// <param name="b"></param>
        /// <param name="transformB"></param>
        /// <returns>Distance</returns>
        public static float Distance(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB
        )
        {
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
                DebugDraw.DrawGjkIteration(wCount, setW, v, w);
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

                if (wCount >= 3 || vkLengthSquared <= Tolerance * maxWLengthSquared)
                {
                    // We regard v as zero
                    return 0f;
                }

                if (v == Vector2.Zero) return 0f;
            }

            return v.Length();
        }

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
                w[0] = s1;
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

        /// <summary>
        /// Continuous collision detection.<br/>
        /// </summary>
        /// <returns>Time of collision.</returns>
        public static bool Continuous(
            ICollider a, in Transform transformA, Vector2 translationA,
            ICollider b, in Transform transformB, Vector2 translationB,
            out float t, out Vector2 normal
        )
        {
            var ray = translationA - translationB;
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
            var pCount = 0;
            Span<float> lambda = stackalloc float[3];
            var k = 0;
            while (k < 64)
            {
                k += 1;
                int i;

#if DEBUG_DRAW
                DebugDraw.DrawString($"x{k - 1}", x);
                DebugDraw.DrawPoint(x);
#endif

                // Termination
                var vLengthSquared = v.LengthSquared();
                var maxPxLengthSquared = 0f;
                for (i = 0; i < pCount; i += 1)
                {
                    var ls = (x - setP[i]).LengthSquared();
                    if (ls > maxPxLengthSquared) maxPxLengthSquared = ls;
                }

                if (vLengthSquared <= Tolerance * maxPxLengthSquared) break;

                var p = a.WorldSupport(transformA, v) - b.WorldSupport(transformB, -v);
                var w = x - p;

                var vDotW = Vector2.Dot(v, w);
                if (vDotW > 0f)
                {
                    var vDotR = Vector2.Dot(v, ray);
                    if (vDotR >= 0f) return false;
                    t = t - vDotW / vDotR;
                    x = t * ray;
                    normal = v;
                }

                // Be careful to compute v(conv({x} − Y))
                setP[pCount] = x - p;
                pCount += 1;
                DistanceSv(ref setP, ref lambda, ref pCount);
                v = Vector2.Zero;
                for (i = 0; i < pCount; i += 1)
                {
                    v += lambda[i] * setP[i];
                    // get P from {x} − Y 
                    setP[i] = x - setP[i];
                }
            }
#if DEBUG_DRAW
            DebugDraw.UpdateIterationCounter(k);
#endif

            return true;
        }

        /// <summary>
        /// Intersection detection via MPR
        /// </summary>
        /// <param name="a"></param>
        /// <param name="transformA"></param>
        /// <param name="b"></param>
        /// <param name="transformB"></param>
        /// <returns>Intersection</returns>
        public static bool Intersection(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB
        )
        {
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

            var counter = MaxRefinement;
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

        private const int MaxRefinement = 5;

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
    }
}