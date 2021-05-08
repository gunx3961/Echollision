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
        public static readonly List<Tuple<int, Vector2[]>> DebugSimplexes = new List<Tuple<int, Vector2[]>>();

        public static void Clear()
        {
            DebugPoints.Clear();
            DebugLines.Clear();
            DebugStrings.Clear();
            DebugSimplexes.Clear();
        }

        public static void OnDrawPoint(Vector2 point)
        {
            DebugPoints.Add(new Vector2(point.X, point.Y));
        }

        public static void OnDrawLine(Vector2 start, Vector2 end)
        {
            DebugLines.Add(new Vector2(start.X, start.Y));
            DebugLines.Add(new Vector2(end.X, end.Y));
        }

        public static void OnDrawString(string text, Vector2 position)
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

        public static void OnDrawSimplex(int vertexCount, Span<Vector2> w)
        {
            DebugSimplexes.Add(new Tuple<int, Vector2[]>(vertexCount, w.ToArray()));
        }

    }
}