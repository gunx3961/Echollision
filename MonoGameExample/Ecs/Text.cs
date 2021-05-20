using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameExample.Ecs
{
    public struct Text
    {
        public string Value;
        public Color Color;
        public float Scale;

        public Vector2 MeasureString(SpriteFont font)
        {
            return font.MeasureString(Value) * Scale;
        }
    }
}