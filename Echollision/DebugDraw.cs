using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public static class DebugDraw
    {
        public static event Action<Vector2> DrawPoint = null!;
        public static event Action<Vector2, Vector2> DrawLine = null!;
        public static event Action<string, Vector2> DrawString = null!;

        public static void OnDrawPoint(Vector2 point)
        {
            DrawPoint?.Invoke(point);
        }

        public static void OnDrawLine(Vector2 start, Vector2 end)
        {
            DrawLine?.Invoke(start, end);
        }

        public static void OnDrawString(string text, Vector2 position)
        {
            DrawString?.Invoke(text, position);
        }
    }
}