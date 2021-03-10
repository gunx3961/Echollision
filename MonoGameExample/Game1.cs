using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.Collider;
using PrimitiveType = ViLAWAVE.Echollision.PrimitiveType;
using SystemVector2 = System.Numerics.Vector2;

namespace MonoGameExample
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private readonly Color _bgColor = new Color(30, 30, 30);
        private Point _logicalSize = new Point(1280, 720);
        private SpriteFont _defaultFont;

        private Vector2 _positionA = new Vector2(400, 320);
        private Vector2 _positionB = new Vector2(420, 250);

        // new
        private bool _isCollide = false;
        private ICollider _colliderA;
        private ICollider _colliderB;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000f / 60);
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

            _colliderA = new MinkowskiSumCollider(new ICollider[]
            {
                new SphereCollider(100),
                new SegmentCollider(new SystemVector2(-100, -100), new SystemVector2(200, 200))
            });
            _colliderB = new ConvexCollider(new SystemVector2[]
            {
                new SystemVector2(-100, -100),
                new SystemVector2(100, -100),
                new SystemVector2(100, 100),
                new SystemVector2(-100, 100)
            });

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            _pixel = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixel.SetData(new[] {Color.White});
            _defaultFont = Content.Load<SpriteFont>("04B09");

            DebugDraw.DrawPoint += HandleDrawDebugPoint;
            DebugDraw.DrawLine += HandleDrawDebugLine;
            DebugDraw.DrawString += HandleDrawDebugString;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            var mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                _positionA = mouseState.Position.ToVector2();
            }

            var translationA = new SystemVector2(_positionA.X, _positionA.Y);
            var transformA = new Transform(translationA, 0);
            var translationB = new SystemVector2(_positionB.X, _positionB.Y);
            var transformB = new Transform(translationB, 0);
            _isCollide = Collision.Detect(_colliderA, transformA, _colliderB, transformB);

            base.Update(gameTime);
        }

        private static readonly Color ColorA = Color.Aqua;
        private static readonly Color ColorB = Color.Orange;
        private static readonly Color ColorBSubA = Color.White;
        private static readonly Color ColorCollision = Color.Yellow;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_bgColor);

            // TODO: Add your drawing code here
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            DrawUI(gameTime);

            // A
            var translationA = new SystemVector2(_positionA.X, _positionA.Y);
            var transformA = new Transform(translationA, 0);
            DrawCollider(_colliderA, transformA, ColorA);

            // B
            var translationB = new SystemVector2(_positionB.X, _positionB.Y);
            var transformB = new Transform(translationB, 0);
            DrawCollider(_colliderB, transformB, ColorB);

            // B-A
            var bSubAOrigin = _logicalSize.ToVector2() * 2 / 3;
            DrawMinkowskiDifference(_colliderA, transformA, _colliderB, transformB, bSubAOrigin,
                _isCollide ? ColorCollision : ColorBSubA);

            // Debug draws
            for (var i = 0; i < _debugLines.Count; i += 2)
            {
                _spriteBatch.DrawLine(_debugLines[i] + bSubAOrigin, _debugLines[i + 1] + bSubAOrigin, Color.Green);
            }

            _debugLines.Clear();

            for (var i = 0; i < _debugPoints.Count; i += 1)
            {
                DrawCross(_debugPoints[i] + bSubAOrigin, Color.LightGreen);
            }

            _debugPoints.Clear();

            for (var i = 0; i < _debugStrings.Count; i += 1)
            {
                _spriteBatch.DrawString(_defaultFont, _debugStrings[i].Item1,
                    _debugStrings[i].Item2 + bSubAOrigin + new Vector2(2, 2), Color.LightGreen);
            }

            _debugStrings.Clear();

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private Texture2D _pixel;
        private ReadOnlyMemory<SystemVector2> _sampleNormals;
        private const int SampleRate = 128;

        private void DrawCollider(ICollider collider, Transform transform, Color color)
        {
            Span<Vector2> samplePoints = stackalloc Vector2[_sampleNormals.Length];

            var samples = _sampleNormals.Span;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = collider.WorldSupport(transform, samples[i]);
                var worldPosition = new Vector2(support.X, support.Y);
                samplePoints[i] = worldPosition;
            }

            DrawShapeOfSamplePoints(samplePoints, color);
        }

        private void DrawMinkowskiDifference(ICollider a, Transform ta, ICollider b, Transform tb, Vector2 originAt,
            Color color)
        {
            Span<Vector2> samplePoints = stackalloc Vector2[_sampleNormals.Length];
            var samples = _sampleNormals.Span;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = Collision.SupportOfMinkowskiDifference(a, ta, b, tb, samples[i]);
                var worldPosition = new Vector2(support.X, support.Y) + originAt;
                samplePoints[i] = worldPosition;
            }

            DrawShapeOfSamplePoints(samplePoints, color);
        }

        private void DrawShapeOfSamplePoints(in Span<Vector2> points, Color color)
        {
            var prev = Vector2.Zero;
            Vector2 first = Vector2.Zero, last = Vector2.Zero;
            for (var i = 0; i < points.Length; i += 1)
            {
                var position = points[i];
                _spriteBatch.Draw(_pixel, position, null, color);

                if (i > 0 && (position - prev).LengthSquared() > 100)
                {
                    _spriteBatch.DrawLine(position, prev, color, 1);
                }

                prev = position;

                if (i == 0) first = position;
                if (i == points.Length - 1) last = position;
            }

            if ((first - last).LengthSquared() > 100)
            {
                _spriteBatch.DrawLine(first, last, color, 1);
            }
        }

        private void DrawCross(Vector2 position, Color color)
        {
            _spriteBatch.Draw(_pixel, position + new Vector2(-2, -2), null, color);
            _spriteBatch.Draw(_pixel, position + new Vector2(2, -2), null, color);
            _spriteBatch.Draw(_pixel, position + new Vector2(-1, -1), null, color);
            _spriteBatch.Draw(_pixel, position + new Vector2(1, -1), null, color);
            _spriteBatch.Draw(_pixel, position, null, color);
            _spriteBatch.Draw(_pixel, position + new Vector2(-1, 1), null, color);
            _spriteBatch.Draw(_pixel, position + new Vector2(1, 1), null, color);
            _spriteBatch.Draw(_pixel, position + new Vector2(-2, 2), null, color);
            _spriteBatch.Draw(_pixel, position + new Vector2(2, 2), null, color);
        }

        private void DrawUI(GameTime gameTime)
        {
            const string asciiTestString =
                " !\"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

            _spriteBatch.DrawString(_defaultFont, asciiTestString, Vector2.Zero, Color.LightGray, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);

            var stringPosition = new Vector2(0, _defaultFont.LineSpacing * 4);
            _spriteBatch.DrawString(_defaultFont, "# Shape A", stringPosition, ColorA, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);

            stringPosition += new Vector2(0, _defaultFont.LineSpacing * 2);
            _spriteBatch.DrawString(_defaultFont, "# Shape B", stringPosition, ColorB, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);

            stringPosition += new Vector2(0, _defaultFont.LineSpacing * 2);
            _spriteBatch.DrawString(_defaultFont, "# Minkowski Difference B-A", stringPosition, ColorBSubA, 0,
                Vector2.Zero, 2,
                SpriteEffects.None, 0);

            stringPosition += new Vector2(0, _defaultFont.LineSpacing * 2);
            _spriteBatch.DrawString(_defaultFont, "# Collision", stringPosition, ColorCollision, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);

            var fps = Math.Ceiling(1.0 / gameTime.ElapsedGameTime.TotalSeconds);
            var fpsText = $"FPS:{fps.ToString()}";
            stringPosition = new Vector2(_logicalSize.X, _defaultFont.LineSpacing * 6) -
                             _defaultFont.MeasureString(fpsText) * 2f;
            _spriteBatch.DrawString(_defaultFont, fpsText, stringPosition, Color.LightGray, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);

            const string note = "@ ViLAWAVE.Echollision MPR collision detection";
            var noteSize = _defaultFont.MeasureString(note);
            _spriteBatch.DrawString(_defaultFont, note,
                _logicalSize.ToVector2() - noteSize * 2 - new Vector2(0, _defaultFont.LineSpacing * 2), Color.LightGray,
                0,
                Vector2.Zero, 2, SpriteEffects.None, 0);

            var testSize = _defaultFont.MeasureString(asciiTestString);
            _spriteBatch.DrawString(_defaultFont, asciiTestString, _logicalSize.ToVector2() - testSize * 2,
                Color.LightGray, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);
        }

        private List<Vector2> _debugPoints = new List<Vector2>();
        private List<Vector2> _debugLines = new List<Vector2>();
        private List<Tuple<string, Vector2>> _debugStrings = new List<Tuple<string, Vector2>>();

        private void HandleDrawDebugPoint(SystemVector2 point)
        {
            _debugPoints.Add(new Vector2(point.X, point.Y));
        }

        private void HandleDrawDebugLine(SystemVector2 start, SystemVector2 end)
        {
            _debugLines.Add(new Vector2(start.X, start.Y));
            _debugLines.Add(new Vector2(end.X, end.Y));
        }

        private void HandleDrawDebugString(string text, SystemVector2 position)
        {
            _debugStrings.Add(new Tuple<string, Vector2>(text, new Vector2(position.X, position.Y)));
        }
    }
}