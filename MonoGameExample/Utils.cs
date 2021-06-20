using System;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Shapes;
using MonoGameExample.Ecs;
using ViLAWAVE.Echollision;

namespace MonoGameExample
{
    public static class Utils
    {
        public static void BeginPixelPerfect(this SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        }

        public static Polygon SampleShape(this Collider collider, int sampleCount = 16)
        {
            Span<Vector2> buffer = stackalloc Vector2[sampleCount];
            var vertCount = 0;
            
            for (var i = 0; i < sampleCount; i += 1)
            {
                var angle = Math.PI * i / sampleCount;
                var n = new Vector2((float) Math.Cos(angle), (float) Math.Sin(angle));
                var vert = collider.Support(n);
                if (vertCount > 0 && buffer[vertCount] == vert) continue;

                buffer[vertCount] = vert;
                vertCount += 1;
            }

            var verts = new Microsoft.Xna.Framework.Vector2[vertCount];
            for (var i = 0; i < vertCount; i += 1)
            {
                verts[i] = buffer[i].ToXnaVector2();
            }


            return new Polygon(verts);
        }
    }
}