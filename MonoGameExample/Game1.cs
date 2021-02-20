using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ViLAWAVE.Echollision;
using PrimitiveType = ViLAWAVE.Echollision.PrimitiveType;
using SystemVector2 = System.Numerics.Vector2;

namespace MonoGameExample
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Color _bgColor = new Color(30, 30, 30);
        private Point _logicalSize = new Point(1280, 720);

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.PreferredBackBufferWidth = _logicalSize.X;
            _graphics.PreferredBackBufferHeight = _logicalSize.Y;
            _graphics.ApplyChanges();

            var normals = new System.Numerics.Vector2[SampleRate];
            for (var i = 0; i < SampleRate; i += 1)
            {
                var radius = MathF.PI * 2 * (i / (float) SampleRate);
                var x = MathF.Sin(radius);
                var y = MathF.Cos(radius);
                var n = SystemVector2.Normalize(new SystemVector2(x, y));
                normals[i] = n;
            }

            _sampleNormals = normals;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            _pixel = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixel.SetData(new[] {Color.White});
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_bgColor);

            // TODO: Add your drawing code here
            _spriteBatch.Begin();


            var sphereShape = new Shape(ShapeType.Primitive, stackalloc Primitive[1]
            {
                new Primitive(PrimitiveType.Sphere, 64, 0, 0.3f * MathF.PI, new SystemVector2(400, 320))
            });
            DrawSupportMapping(sphereShape);

            var capsuleShape = new Shape(ShapeType.MinkowskiSum, stackalloc Primitive[2]
            {
                new Primitive(PrimitiveType.Sphere, 64, 0, 0, new SystemVector2(0, 0)),
                new Primitive(PrimitiveType.Segment, 90, 0, 0.1f * MathF.PI, new SystemVector2(420, 250))
            });
            DrawSupportMapping(capsuleShape);

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private Texture2D _pixel;
        private ReadOnlyMemory<SystemVector2> _sampleNormals;
        private const int SampleRate = 64;

        private void DrawSupportMapping(in Shape shape)
        {
            var samples = _sampleNormals.Span;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = SupportMapping.Support(shape, samples[i]);
                var worldPosition = new Vector2(support.X, support.Y);
                _spriteBatch.Draw(_pixel, worldPosition, null, Color.White);
            }
        }
    }
}