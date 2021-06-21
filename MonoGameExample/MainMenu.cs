using System;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input;
using MonoGameExample.Ecs;
using ViLAWAVE.Echollision;


namespace MonoGameExample
{
    public class MainMenu : Screen
    {
        public MainMenu(Framework framework) : base(framework)
        {
        }

        private DefaultEcs.System.ISystem<TimeSpan> _drawSystem;
        private DefaultEcs.System.ISystem<float> _logicalSystem;

        public override void Initialize()
        {
            base.Initialize();

            _logicalSystem = new ActionSystem<float>(HandleClick);
            _drawSystem = new DrawSystem(Framework, SpriteBatch, World);

            var narrowPhaseButton = World.CreateEntity();
            narrowPhaseButton.Set(new Transform2D() {Position = new System.Numerics.Vector2(160, 160)});
            var text = new Text() {Value = "Narrow Phase", Color = Color.LightGray, Scale = 7f};
            narrowPhaseButton.Set(text);
            var size = text.MeasureString(DefaultFont);
            var collider = ColliderFactory.Rect(size);
            narrowPhaseButton.Set(collider);
            narrowPhaseButton.Set<Action>(() => { Framework.ScreenManager.LaunchNarrowPhase(); });

            var boardPhaseButton = World.CreateEntity();
            boardPhaseButton.Set(new Transform2D() {Position = new System.Numerics.Vector2(480, 320)});
            text = new Text() {Value = "Board Phase", Color = Color.LightGray, Scale = 7f};
            boardPhaseButton.Set(text);
            size = text.MeasureString(DefaultFont);
            collider = ColliderFactory.Rect(size);
            boardPhaseButton.Set(collider);
            boardPhaseButton.Set<Action>(() => { Framework.ScreenManager.LaunchBroadPhase(); });


            var playgroundButton = World.CreateEntity();
            playgroundButton.Set(new Transform2D() {Position = new System.Numerics.Vector2(800, 480)});
            text = new Text() {Value = "Playground", Color = Color.LightGray, Scale = 7f};
            playgroundButton.Set(text);
            size = text.MeasureString(DefaultFont);
            collider = ColliderFactory.Rect(size);
            playgroundButton.Set(collider);
            playgroundButton.Set<Action>(() => { Framework.ScreenManager.LaunchPlayground(); });
        }

        public override void Update(GameTime gameTime)
        {
            _logicalSystem.Update(0f);
        }

        private Color _bgColor = new Color(30, 30, 30);

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_bgColor);

            _drawSystem.Update(gameTime.TotalGameTime);

            // SpriteBatch.BeginPixelPerfect();
            // // SpriteBatch.DrawString(DefaultFont, "Narrow Phase", new Vector2(160, 160), Color.LightGray, 0f,
            // //     Vector2.Zero, 5, SpriteEffects.None, 0);
            // // SpriteBatch.DrawString(DefaultFont, "Board Phase", new Vector2(160, 240), Color.LightGray, 0f,
            // //     Vector2.Zero, 5, SpriteEffects.None, 0);
            //
            // var mouseX = Framework.MouseState.Position.X;
            // var mouseY = Framework.MouseState.Position.Y;
            // SpriteBatch.DrawLine(mouseX, 0f, mouseX, Framework.LogicalSize.Y, Color.Gray);
            // SpriteBatch.DrawLine(0f, mouseY, Framework.LogicalSize.X, mouseY, Color.Gray);
            //
            // SpriteBatch.End();
        }

        private void HandleClick(float time)
        {
            if (!Framework.MouseState.WasButtonJustDown(MouseButton.Left)) return;
            var mousePosition = Framework.MouseState.Position.ToVector2().ToSystemVector2();
            var pointCollider = ColliderFactory.Point();

            var clickableSet = World.GetEntities().With<Text>().AsEnumerable();
            foreach (var clickable in clickableSet)
            {
                var transform = clickable.Get<Transform2D>().ToCollisionTransform();
                var collider = clickable.Get<ConvexCollider>();
                var hit = Framework.Collision.Intersection(collider, transform, pointCollider,
                    new ColliderTransform(mousePosition, 0f));

                if (!hit) continue;

                clickable.Get<Action>()();
                break;
            }
        }
    }
}
