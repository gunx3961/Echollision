using System;
using System.Collections.Generic;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    internal struct GjkProcedure
    {
        public int VertexCount;
        public Vector2[] W;
        public Vector2 V;
        public Vector2 NewW;
    }

    internal struct GjkRayCastProcedure
    {
        public Vector2 X;
        public Vector2 P;
        public int VertexCount;
        public Vector2[] SetP;
        public Vector2 V;
    }

    internal struct MprProcedure
    {
        public Vector2 V0;
        public Vector2 V1;
        public Vector2 V2;
        public Vector2 V3;
    }
    
    internal struct GjkRayCastContext
    {
        public Vector2 Ray;
    }
    
    internal struct PenetrationContext
    {
        public Vector2 PointA;
        public Vector2 PointB;
        public Vector2 Normal;
    }

    
    internal class CollisionDetail
    {
        

        internal int IterationCounter { get; private set; }
        internal readonly List<GjkProcedure> GjkProcedures = new List<GjkProcedure>();
        internal readonly List<GjkRayCastProcedure> GjkRayCastProcedures = new List<GjkRayCastProcedure>();
        internal readonly List<MprProcedure> MprProcedures = new List<MprProcedure>();
        internal GjkRayCastContext GjkRayCastContext;
        internal PenetrationContext PenetrationContext;

        internal void Clear()
        {
            IterationCounter = 0;
            GjkProcedures.Clear();
            GjkRayCastProcedures.Clear();
            MprProcedures.Clear();
        }

        internal void PushGjkProcedure(int vertexCount, Span<Vector2> w, Vector2 v, Vector2 newW)
        {
            GjkProcedures.Add(new GjkProcedure {VertexCount = vertexCount, W = w.ToArray(), V = v, NewW = newW});
        }

        internal void PushGjkRayCastProcedure(Vector2 x, Vector2 p, int vertexCount, Span<Vector2> setP, Vector2 v)
        {
            GjkRayCastProcedures.Add(new GjkRayCastProcedure {X = x, P = p, VertexCount = vertexCount, SetP = setP.ToArray(), V = v});
        }

        internal void PushMprProcedure(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            MprProcedures.Add(new MprProcedure {V0 = v0, V1 = v1, V2 = v2, V3 = v3});
        }

        internal void UpdatePenetrationContext(Vector2 aPoint, Vector2 bPoint, Vector2 normal)
        {
            PenetrationContext.PointA = aPoint;
            PenetrationContext.PointB = bPoint;
            PenetrationContext.Normal = normal;
        }
        
        internal void UpdateIterationCounter(int count)
        {
            IterationCounter = count;
        }
    }
}
