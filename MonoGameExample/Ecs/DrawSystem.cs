using System;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameExample.Ecs
{
    [With(typeof(Text))]
    [With(typeof(Transform2D))]
    public class DrawSystem : AEntitySetSystem<TimeSpan>
    {
        private readonly Framework _framework;
        private readonly SpriteBatch _batch;

        public DrawSystem(Framework framework, SpriteBatch batch, World world) : base(world)
        {
            _framework = framework;
            _batch = batch;
        }

        protected override void PreUpdate(TimeSpan gameTime)
        {
            _batch.BeginPixelPerfect();
        }

        protected override void Update(TimeSpan gameTime, in Entity entity)
        {
            var font = _framework.Resource.GetFont(GlobalResource.Font.DefaultPixel);
            ref var text = ref entity.Get<Text>();
            ref var transform = ref entity.Get<Transform2D>();
            _batch.DrawString(font, text.Value, transform.Position, text.Color, transform.Rotation, Vector2.Zero,
                text.Scale, SpriteEffects.None, 0f);
        }

        protected override void PostUpdate(TimeSpan state)
        {
            _batch.End();
        }
    }
}