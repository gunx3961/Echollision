using System;
using System.Collections.Generic;
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
        private SpriteFont _defaultFont;

        private Vector2 _positionA = new Vector2(400, 320);
        private Vector2 _positionB = new Vector2(420, 250);

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
                var radius = MathF.PI * 2 * (i / (float)SampleRate);
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
            _pixel.SetData(new[] { Color.White });
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
            // var sphereShape = new Shape(ShapeType.Primitive, stackalloc Primitive[1]
            // {
            //     new Primitive(PrimitiveType.Sphere, 64, 0, 0.3f * MathF.PI, new SystemVector2(_positionA.X, _positionA.Y))
            // });
            var sphereShape = new Shape(ShapeType.MaxSupport, stackalloc Primitive[3]
            {
                new Primitive(PrimitiveType.Point, 0, 0, 0, new SystemVector2(_positionA.X - 25, _positionA.Y - 80)),
                new Primitive(PrimitiveType.Point, 0, 0, 0, new SystemVector2(_positionA.X + 75, _positionA.Y + 65)),
                new Primitive(PrimitiveType.Point, 0, 0, 0, new SystemVector2(_positionA.X - 40, _positionA.Y + 55)),
            });
            DrawSupportMapping(sphereShape, ColorA);

            // B
            var capsuleShape = new Shape(ShapeType.MinkowskiSum, stackalloc Primitive[2]
            {
                new Primitive(PrimitiveType.Sphere, 64, 0, 0, new SystemVector2(0, 0)),
                new Primitive(PrimitiveType.Segment, 90, 0, 0.1f * MathF.PI, new SystemVector2(420, 250))
            });
            DrawSupportMapping(capsuleShape, ColorB);

            // B-A
            var result = MPR.Detect(sphereShape, capsuleShape);
            var bSubAOrigin = _logicalSize.ToVector2() / 2;
            DrawSupportMappingBSubtractA(sphereShape, capsuleShape, bSubAOrigin, result ? ColorCollision : ColorBSubA);


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
                _spriteBatch.DrawString(_defaultFont, _debugStrings[i].Item1, _debugStrings[i].Item2 + bSubAOrigin + new Vector2(2, 2), Color.LightGreen);
            }
            _debugStrings.Clear();


            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private Texture2D _pixel;
        private ReadOnlyMemory<SystemVector2> _sampleNormals;
        private const int SampleRate = 128;

        private void DrawSupportMapping(in Shape shape, Color color)
        {
            var samples = _sampleNormals.Span;
            var lastPosition = Vector2.Zero;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = SupportMapping.Support(shape, samples[i]);
                var worldPosition = new Vector2(support.X, support.Y);
                _spriteBatch.Draw(_pixel, worldPosition, null, color);
                if (i > 0 && (worldPosition - lastPosition).LengthSquared() > 100)
                {
                    _spriteBatch.DrawLine(worldPosition, lastPosition, color, 0.4f);
                }
                lastPosition = worldPosition;
            }
        }

        private void DrawSupportMappingBSubtractA(in Shape a, in Shape b, Vector2 origin, Color color)
        {
            var samples = _sampleNormals.Span;
            var lastPosition = Vector2.Zero;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = SupportMapping.SupportOfMinkowskiDifference(a, b, samples[i]);
                var worldPosition = new Vector2(support.X, support.Y) + origin;
                _spriteBatch.Draw(_pixel, worldPosition, null, color);

                if (i > 0 && (worldPosition - lastPosition).LengthSquared() > 100)
                {
                    _spriteBatch.DrawLine(worldPosition, lastPosition, color, 0.4f);
                }
                lastPosition = worldPosition;
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
            stringPosition = new Vector2(_logicalSize.X, _defaultFont.LineSpacing * 6) - _defaultFont.MeasureString(fpsText) * 2f;
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