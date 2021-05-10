using System;
using System.Diagnostics;
using System.Numerics;
using ViLAWAVE.Echollision.Collider;

namespace ViLAWAVE.Echollision
{
    public static class Collision
    {
        private const float Tolerance = 1e-8f; // [van der Bergen 2003] P.143
        private const float RelativeErrorTolerance = 1e-8f;

        public static float DetectGjk(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB
        )
        {
            var k = 0;
            DebugDraw.Clear();
            DebugDraw.DrawString("origin", Vector2.Zero);
            DebugDraw.DrawPoint(Vector2.Zero);
            // Pick arbitrary support point as initial v
            var v = SupportOfMinkowskiDifference(a, transformA, b, transformB, Vector2.UnitX);

            Span<Vector2> tau = stackalloc Vector2[3];
            Span<float> lambda = stackalloc float[3];
            var vertexCount = 0;

            while (k < 24)
            {
                k += 1;
                var negativeVDirection = Vector2.Normalize(-v) * 100;
                DebugDraw.DrawLine(Vector2.Zero, negativeVDirection);
                DebugDraw.DrawString($"-v{(k - 1).ToString()}", negativeVDirection);
                var w = SupportOfMinkowskiDifference(a, transformA, b, transformB, -v);
                // DebugDraw.DrawPoint(w);
                // DebugDraw.DrawString($"w{(k - 1).ToString()}", w);
                int i;
                for (i = 0; i < vertexCount; i++)
                {
                    if ((w - tau[i]).LengthSquared() < RelativeErrorTolerance) return v.Length();
                    // if (w == tau[i]) return v.Length();
                }

                var vkLengthSquared = v.LengthSquared();
                var vDotW = Vector2.Dot(v, w);
                var vIsCloseToVFactor = RelativeErrorTolerance * vkLengthSquared;
                if (vkLengthSquared - vDotW <= vIsCloseToVFactor)
                {
                    return v.Length();
                }

                tau[vertexCount] = w;
                vertexCount += 1;

                DistanceSv(ref tau, ref lambda, ref vertexCount);
                v = Vector2.Zero;
                for (i = 0; i < vertexCount; i++)
                {
                    v += lambda[i] * tau[i];
                }
                
                DebugDraw.DrawGjkIteration(vertexCount, tau, v, w);
                DebugDraw.DrawPoint(v);
                DebugDraw.DrawString($"v{k}", v);

                // Termination
                var maxWLengthSquared = tau[0].LengthSquared();
                for (i = 1; i < vertexCount; i++)
                {
                    var wls = tau[i].LengthSquared();
                    if (wls > maxWLengthSquared) maxWLengthSquared = wls;
                }
                if (vertexCount >= 3 || vkLengthSquared <= Tolerance * maxWLengthSquared)
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
                    S1DMyVersion(ref tau, ref lambda, ref vertexCount);
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

            var flag = 0b000;
            if (IsSameSign(detM, cofactor31)) flag |= 0b100;
            if (IsSameSign(detM, cofactor32)) flag |= 0b010;
            if (IsSameSign(detM, cofactor33)) flag |= 0b001;


            switch (flag)
            {
                case 0b111:
                    // Origin is inside the 2-simplex 
                    lambda[0] = cofactor31 / detM;
                    lambda[1] = cofactor32 / detM;
                    lambda[2] = cofactor33 / detM;
                    vertexCount = 3;
                    break;

                // FIXME: we should compare each S1D result
                case 0b100:
                    w[0] = s1;
                    lambda[0] = 1f;
                    vertexCount = 1;
                    break;
                case 0b010:
                    w[0] = s2;
                    lambda[0] = 1f;
                    vertexCount = 1;
                    break;
                case 0b001:
                    w[0] = s3;
                    lambda[0] = 1f;
                    vertexCount = 1;
                    break;

                case 0b110:
                    w[0] = s1;
                    w[1] = s2;
                    S1DMyVersion(ref w, ref lambda, ref vertexCount);
                    break;
                case 0b101:
                    w[0] = s1;
                    w[1] = s3;
                    S1DMyVersion(ref w, ref lambda, ref vertexCount);
                    break;
                case 0b011:
                    w[0] = s2;
                    w[1] = s3;
                    S1DMyVersion(ref w, ref lambda, ref vertexCount);
                    break;
                
                default:
                    // Ill case of which detM = 0
                    var minLengthSquared = w[0].LengthSquared();
                    var minVertexIndex = 0;
                    for (var vertIndex = 1; vertIndex < 3; vertIndex++)
                    {
                        var ls = w[vertIndex].LengthSquared();
                        if (ls >= minLengthSquared) continue;
                        minLengthSquared = ls;
                        minVertexIndex = vertIndex;
                    }

                    w[0] = w[minVertexIndex];
                    lambda[0] = 1f;
                    vertexCount = 1;
                    // Debugger.Break();
                    // throw new ApplicationException("Assertion Error");
                    break;
            }
        }

        private static void S1D(ref Span<Vector2> w, ref Span<float> lambda, ref int vertexCount)
        {
            var s1 = w[0];
            var s2 = w[1];

            var t = s2 - s1;
            var po = Vector2.Dot(s2, t) / t.LengthSquared() * t + s2;

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

            if (IsSameSign(miuMax, cofactor1) && IsSameSign(miuMax, cofactor2))
            {
                lambda[0] = cofactor1 / miuMax;
                lambda[1] = cofactor2 / miuMax;
                vertexCount = 2;
            }
            else
            {
                lambda[0] = 1f;
                vertexCount = 1;
            }
        }

        private static void S1DMyVersion(ref Span<Vector2> w, ref Span<float> lambda, ref int vertexCount)
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

        public static bool Detect(
            ICollider a, in Transform transformA,
            ICollider b, in Transform transformB
        )
        {
            // DebugDraw.OnDrawString("origin", Vector2.Zero);
            // DebugDraw.OnDrawPoint(Vector2.Zero);

            var centerA = a.WorldCenter(transformA);
            var centerB = b.WorldCenter(transformB);
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) v0 = new Vector2(0.00001f, 0);

            // DebugDraw.OnDrawString("v0", v0);
            // DebugDraw.OnDrawPoint(v0);
            var normal = Vector2.Normalize(-v0);
            // DebugDraw.OnDrawString("origin ray", v0 + normal * 240);
            // DebugDraw.OnDrawLine(v0, v0 + normal * 240);

            var v1 = SupportOfMinkowskiDifference(a, transformA, b, transformB, normal);
            // DebugDraw.OnDrawString("v1", v1);
            // DebugDraw.OnDrawPoint(v1);
            // DebugDraw.OnDrawLine(v0, v1);
            normal = Vector2.Normalize(v1 - v0);
            normal = new Vector2(normal.Y, -normal.X);
            if (Vector2.Dot(-v0, normal) < 0) normal = -normal;

            var v2 = SupportOfMinkowskiDifference(a, transformA, b, transformB, normal);
            // DebugDraw.OnDrawString("v2", v2);
            // DebugDraw.OnDrawPoint(v2);
            // DebugDraw.OnDrawLine(v0, v2);
            // DebugDraw.OnDrawLine(v1, v2);

            var counter = MaxRefinement;
            while (counter > 0)
            {
                normal = Vector2.Normalize(v2 - v1);
                normal = new Vector2(normal.Y, -normal.X);
                if (Vector2.Dot(normal, v0 - v1) > 0) normal = -normal; // Outer normal

                var debugMidPoint = (v1 + v2) / 2;
                // DebugDraw.OnDrawLine(debugMidPoint, debugMidPoint + normal * 100);
                // DebugDraw.OnDrawString("n", debugMidPoint + normal * 100);

                if (Vector2.Dot(normal, -v1) < 0) return true;

                var v3 = SupportOfMinkowskiDifference(a, transformA, b, transformB, normal);
                // DebugDraw.OnDrawLine(v0, v3);

                if (Vector2.Dot(normal, v3) < 0) return false;

                normal = Vector2.Normalize(v3 - v0);
                normal = new Vector2(normal.Y, -normal.X);

                if (Vector2.Dot(v2 - v1, normal) > 0 ^ Vector2.Dot(-v0, normal) > 0) // in v1 side
                {
                    v2 = v3;
                    // DebugDraw.OnDrawLine(v1, v3);
                }
                else
                {
                    v1 = v3;
                    // DebugDraw.OnDrawLine(v2, v3);
                }

                counter -= 1;
            }

            return false;
        }

        public static bool DetectPriori(
            ICollider a, in Transform transformA, Vector2 movementA,
            ICollider b, in Transform transformB, Vector2 movementB
        )
        {
            // DebugDraw.OnDrawString("origin", Vector2.Zero);
            // DebugDraw.OnDrawPoint(Vector2.Zero);

            // Treat collider A as the moving collider
            var relativeMovement = movementA - movementB;
            var centerA = a.WorldCenter(transformA, relativeMovement);
            var centerB = b.WorldCenter(transformB);
            var v0 = centerB - centerA;
            if (v0 == Vector2.Zero) v0 = new Vector2(0.00001f, 0);
            // DebugDraw.OnDrawMovement(v0 - relativeMovement / 2, v0 + relativeMovement / 2);
            //
            // DebugDraw.OnDrawString("v0", v0);
            // DebugDraw.OnDrawPoint(v0);
            var normal = Vector2.Normalize(-v0);
            // DebugDraw.OnDrawString("origin ray", v0 + normal * 240);
            // DebugDraw.OnDrawLine(v0, v0 + normal * 240);

            var v1 = SupportOfMinkowskiDifference(a, transformA, b, transformB, relativeMovement, normal);
            // DebugDraw.OnDrawString("v1", v1);
            // DebugDraw.OnDrawPoint(v1);
            // DebugDraw.OnDrawLine(v0, v1);
            normal = Vector2.Normalize(v1 - v0);
            normal = new Vector2(normal.Y, -normal.X);
            if (Vector2.Dot(-v0, normal) < 0) normal = -normal;

            var v2 = SupportOfMinkowskiDifference(a, transformA, b, transformB, relativeMovement, normal);
            // DebugDraw.OnDrawString("v2", v2);
            // DebugDraw.OnDrawPoint(v2);
            // DebugDraw.OnDrawLine(v0, v2);
            // DebugDraw.OnDrawLine(v1, v2);

            var counter = MaxRefinement;
            while (counter > 0)
            {
                normal = Vector2.Normalize(v2 - v1);
                normal = new Vector2(normal.Y, -normal.X);
                if (Vector2.Dot(normal, v0 - v1) > 0) normal = -normal; // Outer normal

                var debugMidPoint = (v1 + v2) / 2;
                // DebugDraw.OnDrawLine(debugMidPoint, debugMidPoint + normal * 100);
                // DebugDraw.OnDrawString("n", debugMidPoint + normal * 100);

                if (Vector2.Dot(normal, -v1) < 0) return true;

                var v3 = SupportOfMinkowskiDifference(a, transformA, b, transformB, relativeMovement, normal);
                // DebugDraw.OnDrawLine(v0, v3);

                if (Vector2.Dot(normal, v3) < 0) return false;

                normal = Vector2.Normalize(v3 - v0);
                normal = new Vector2(normal.Y, -normal.X);

                if (Vector2.Dot(v2 - v1, normal) > 0 ^ Vector2.Dot(-v0, normal) > 0) // in v1 side
                {
                    v2 = v3;
                    // DebugDraw.OnDrawLine(v1, v3);
                }
                else
                {
                    v1 = v3;
                    // DebugDraw.OnDrawLine(v2, v3);
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

        public static Vector2 WorldSupport(this ICollider shape, in Transform transform, Vector2 movement,
            Vector2 normal)
        {
            var movementSupport = Vector2.Dot(movement, normal) > 0 ? movement : Vector2.Zero;
            var shapeSupport = shape.WorldSupport(transform, normal);
            return shapeSupport + movementSupport;
        }

        public static Vector2 WorldCenter(this ICollider shape, in Transform transform)
        {
            var rotation = Matrix3x2.CreateRotation(transform.Rotation);
            return Vector2.Transform(shape.Center, rotation) + transform.Translation;
        }

        public static Vector2 WorldCenter(this ICollider shape, in Transform transform, Vector2 movement)
        {
            return shape.WorldCenter(transform) + (movement / 2f);
        }

        public static Vector2 SupportOfMinkowskiDifference(
            ICollider a, in Transform ta,
            ICollider b, in Transform tb,
            Vector2 normal
        )
        {
            return b.WorldSupport(tb, normal) - a.WorldSupport(ta, -normal);
        }

        public static Vector2 SupportOfMinkowskiDifference(
            ICollider a, in Transform ta,
            ICollider b, in Transform tb,
            Vector2 relativeMovement, Vector2 normal
        )
        {
            return b.WorldSupport(tb, normal) - a.WorldSupport(ta, relativeMovement, -normal);
        }
    }
}