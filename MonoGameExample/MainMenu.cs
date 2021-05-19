using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input;

namespace MonoGameExample
{
    public class MainMenu : Screen
    {
        public MainMenu(Framework framework) : base(framework)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // TODO: ECS
            if (Framework.MouseState.WasButtonJustDown(MouseButton.Left))
            {
                Framework.ScreenManager.LaunchNarrowPhase();
            }
        }

        private Color _bgColor = new Color(30, 30, 30);

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_bgColor);

            SpriteBatch.BeginPixelPerfect();
            SpriteBatch.DrawString(DefaultFont, "Narrow Phase", new Vector2(160, 160), Color.LightGray, 0f,
                Vector2.Zero, 5, SpriteEffects.None, 0);
            SpriteBatch.DrawString(DefaultFont, "Board Phase", new Vector2(160, 240), Color.LightGray, 0f,
                Vector2.Zero, 5, SpriteEffects.None, 0);

            var mouseX = Framework.MouseState.Position.X;
            var mouseY = Framework.MouseState.Position.Y;
            SpriteBatch.DrawLine(mouseX, 0f, mouseX, Framework.LogicalSize.Y, Color.Gray);
            SpriteBatch.DrawLine(0f, mouseY, Framework.LogicalSize.X, mouseY, Color.Gray);
            
            SpriteBatch.End();
        }
    }
}