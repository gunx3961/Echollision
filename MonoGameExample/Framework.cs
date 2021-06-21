using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Input;
using ViLAWAVE.Echollision;

namespace MonoGameExample
{
    public class Framework : Game
    {
        public Point LogicalSize { get; private set; } = new Point(1440, 840);
        public readonly ScreenManager ScreenManager;
        public readonly GlobalResource Resource;

        private readonly GraphicsDeviceManager _graphics;
        
        // Input
        public MouseStateExtended MouseState { get; private set; }
        public KeyboardStateExtended KeyboardState { get; private set; }
        
        // Collision
        public readonly Collision Collision = new Collision();

        public Framework()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000f / 60);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Resource = new GlobalResource(this);
            
            ScreenManager = new ScreenManager(this);
            Components.Add(ScreenManager);
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = LogicalSize.X;
            _graphics.PreferredBackBufferHeight = LogicalSize.Y;
            _graphics.ApplyChanges();

            base.Initialize();
            
            ScreenManager.LaunchMainMenu();
        }

        protected override void LoadContent()
        {
            Resource.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // Framework update first
            KeyboardState = KeyboardExtended.GetState();
            MouseState = MouseExtended.GetState();
            
            // Here the component updates go 
            base.Update(gameTime);
        }
    }
}