using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameExample
{
    public class Screen : DrawableGameComponent
    {
        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont DefaultFont { get; private set; }
        protected readonly Framework Framework;
        protected World World { get; private set; }
        
        public Screen(Framework framework) : base(framework)
        {
            Framework = framework;
        }

        public override void Initialize()
        {
            // ECS by default
            World = new World();
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            SpriteBatch = new SpriteBatch(GraphicsDevice);
            DefaultFont = Game.Content.Load<SpriteFont>("04B09");
        }
    }
}