using Microsoft.Xna.Framework.Graphics;

namespace MonoGameExample
{
    public static class Utils
    {
        public static void BeginPixelPerfect(this SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        }
    }
}