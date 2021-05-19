using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameExample
{
    public class Framework : Game
    {
        public Point LogicalSize { get; private set; } = new Point(1440, 840);
        public readonly ScreenManager ScreenManager;

        private readonly GraphicsDeviceManager _graphics;

        public Framework()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000f / 60);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            ScreenManager = new ScreenManager(this);
            Components.Add(ScreenManager);
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = LogicalSize.X;
            _graphics.PreferredBackBufferHeight = LogicalSize.Y;
            _graphics.ApplyChanges();

            base.Initialize();
            
            ScreenManager.LaunchNarrowPhase();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}