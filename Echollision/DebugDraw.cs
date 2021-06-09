using System;
using System.Collections.Generic;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public static class DebugDraw
    {
        public static readonly List<Vector2> DebugPoints = new List<Vector2>();
        public static readonly List<Vector2> DebugLines = new List<Vector2>();
        public static readonly List<Tuple<string, Vector2>> DebugStrings = new List<Tuple<string, Vector2>>();
        public static int IterationCounter { get; private set; } = 0;

        public static readonly List<Tuple<int, Vector2[], Vector2, Vector2>> GjkProcedure =
            new List<Tuple<int, Vector2[], Vector2, Vector2>>();

        public static readonly List<Tuple<Vector2, Vector2, int, Vector2[], Vector2>> GjkRayCastProcedure =
            new List<Tuple<Vector2, Vector2, int, Vector2[], Vector2>>();

        public static readonly List<(Vector2, Vector2, Vector2, Vector2)> MprProcedure =
            new List<(Vector2, Vector2, Vector2, Vector2)>();
        
        public static Vector2 PenetrationA { get; private set; } = Vector2.Zero;
        public static Vector2 PenetrationB { get; private set; } = Vector2.Zero;
        public static Vector2 PenetrationNormal { get; private set; } = Vector2.One;

        internal static void Clear()
        {
            DebugPoints.Clear();
            DebugLines.Clear();
            DebugStrings.Clear();
            GjkProcedure.Clear();
            GjkRayCastProcedure.Clear();
            MprProcedure.Clear();
            IterationCounter = 0;
        }

        internal static void DrawPoint(Vector2 point)
        {
            DebugPoints.Add(new Vector2(point.X, point.Y));
        }

        internal static void DrawLine(Vector2 start, Vector2 end)
        {
            DebugLines.Add(new Vector2(start.X, start.Y));
            DebugLines.Add(new Vector2(end.X, end.Y));
        }

        internal static void DrawString(string text, Vector2 position)
        {
            DebugStrings.Add(new Tuple<string, Vector2>(text, new Vector2(position.X, position.Y)));
        }

        // public static void OnDrawMovement(Vector2 start, Vector2 end)
        // {
        //     var ratioPoint = start + (end - start) * Ratio;
        //     _debugLines.Add(new Vector2(start.X, start.Y));
        //     _debugLines.Add(new Vector2(end.X, end.Y));
        //     _debugPoints.Add(ratioPoint.ToXnaVector2());
        // }

        internal static void DrawGjkProcedure(int vertexCount, Span<Vector2> w, Vector2 v, Vector2 newW)
        {
            GjkProcedure.Add(new Tuple<int, Vector2[], Vector2, Vector2>(vertexCount, w.ToArray(), v, newW));
        }

        internal static void DrawGjkRayCastProcedure(Vector2 x, Vector2 p, int vertexCount, Span<Vector2> setX,
            Vector2 v)
        {
            GjkRayCastProcedure.Add(
                new Tuple<Vector2, Vector2, int, Vector2[], Vector2>(x, p, vertexCount, setX.ToArray(), v));
        }

        internal static void DrawMprProcedure(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            MprProcedure.Add((v0, v1, v2, v3));
        }

        internal static void DrawPenetration(Vector2 aPoint, Vector2 bPoint, Vector2 normal)
        {
            PenetrationA = aPoint;
            PenetrationB = bPoint;
            PenetrationNormal = normal;
        }

        internal static void UpdateIterationCounter(int count)
        {
            IterationCounter = count;
        }
    }
}