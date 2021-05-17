using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.Collider;
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

        private enum ControlMode
        {
            None,
            Position,
            Movement,
            Ratio
        }

        private enum ColliderTarget
        {
            None,
            A,
            B
        }

        private ControlMode _controlMode = ControlMode.None;
        private ColliderTarget _colliderTarget = ColliderTarget.None;
        private Point _controlAnchor = Point.Zero;
        private readonly ICollider _pointer = new SphereCollider(0);
        private bool _isCollide = false;
        private float _distance = 0f;
        private float _time = 1f;
        private ICollider _colliderA;
        private ICollider _colliderB;
        private int _debugCursor = 0;

        // Position
        private Vector2 _positionABase = new Vector2(196, 469);
        private Vector2 _positionAControl = Vector2.Zero;
        private Vector2 PositionA => _positionABase + _positionAControl;

        private Vector2 _positionBBase = new Vector2(420, 250);
        private Vector2 _positionBControl = Vector2.Zero;
        private Vector2 PositionB => _positionBBase + _positionBControl;

        // Movement
        private Vector2 _movementA = new Vector2(0, 0);

        private Vector2 _movementB = new Vector2(0, 0);

        // Ratio
        private float _ratioBase = 0f;
        private float _ratioControl = 0f;
        private float Ratio => Math.Clamp(_ratioBase + _ratioControl, 0f, 1f);

        private Transform TransformA => new Transform(PositionA.ToSystemVector2(), 0);
        private Transform TransformAWithCurrentMovement => new Transform((PositionA + _movementA * Ratio).ToSystemVector2(), 0);
        private Transform TransformB => new Transform(PositionB.ToSystemVector2(), 0);
        private Transform TransformBWithCurrentMovement => new Transform((PositionB + _movementB * Ratio).ToSystemVector2(), 0);


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

            _colliderA = new SphereCollider(100);
            // _colliderA = new ConvexCollider(new SystemVector2[]
            // {
            //     new SystemVector2(-200, -100),
            //     new SystemVector2(200, -100),
            //     new SystemVector2(100, 100),
            // });

            // _colliderA = new ConvexHullCollider(new ICollider[]
            // {
            //     new SphereCollider(200),
            //     new ConvexCollider(new SystemVector2[]
            //     {
            //         new SystemVector2(-200, -100),
            //         new SystemVector2(200, -100),
            //         new SystemVector2(100, 100),
            //     })
            // });

            _colliderB = new SphereCollider(200);
            // _colliderB = new SphereCollider(0);
            // _colliderB = new ConvexCollider(new SystemVector2[]
            // {
            //     new SystemVector2(-100, -100),
            //     new SystemVector2(100, -100),
            //     new SystemVector2(100, 100),
            //     new SystemVector2(-100, 100)
            // });

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixel.SetData(new[] {Color.White});
            _defaultFont = Content.Load<SpriteFont>("04B09");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Debug cursor
            var keyboardState = KeyboardExtended.GetState();
            if (keyboardState.WasKeyJustDown(Keys.R)) _debugCursor = 0;

            if (keyboardState.WasKeyJustDown(Keys.Left))
            {
                _debugCursor = Math.Clamp(_debugCursor - 1, 0, Int32.MaxValue);
            }
            else if (keyboardState.WasKeyJustDown(Keys.Right))
            {
                _debugCursor = Math.Clamp(_debugCursor + 1, 0, Int32.MaxValue);
            }

            // Dump
            if (keyboardState.WasKeyJustDown(Keys.T))
            {
                System.Diagnostics.Debug.WriteLine($"PositionA: new Vector2({PositionA.X}, {PositionA.Y})");
                System.Diagnostics.Debug.WriteLine($"PositionB: new Vector2({PositionB.X}, {PositionB.Y})");
            }

            var mouseState = MouseExtended.GetState();
            switch (_controlMode)
            {
                case ControlMode.None when mouseState.WasButtonJustDown(MouseButton.Left):
                    _controlMode = ControlMode.Movement;
                    _colliderTarget = DetermineTarget(mouseState.Position);
                    _controlAnchor = mouseState.Position;
                    break;
                case ControlMode.None when mouseState.WasButtonJustDown(MouseButton.Middle):
                    _controlMode = ControlMode.Position;
                    _colliderTarget = DetermineTarget(mouseState.Position);
                    _controlAnchor = mouseState.Position;
                    break;
                case ControlMode.None when mouseState.WasButtonJustDown(MouseButton.Right):
                    _controlMode = ControlMode.Ratio;
                    _controlAnchor = mouseState.Position;
                    break;

                case ControlMode.Movement when mouseState.WasButtonJustUp(MouseButton.Left):
                case ControlMode.Position when mouseState.WasButtonJustUp(MouseButton.Middle):
                case ControlMode.Ratio when mouseState.WasButtonJustUp(MouseButton.Right):
                    _controlMode = ControlMode.None;
                    _colliderTarget = ColliderTarget.None;
                    _controlAnchor = Point.Zero;

                    _positionABase = PositionA;
                    _positionAControl = Vector2.Zero;
                    _positionBBase = PositionB;
                    _positionBControl = Vector2.Zero;

                    _ratioBase = Ratio;
                    _ratioControl = 0;

                    break;
            }

            var controlVector = (mouseState.Position - _controlAnchor).ToVector2();

            switch (_controlMode)
            {
                case ControlMode.Movement when _colliderTarget == ColliderTarget.A:
                    _movementA = controlVector;
                    _debugCursor = 0;
                    break;
                case ControlMode.Movement when _colliderTarget == ColliderTarget.B:
                    _movementB = controlVector;
                    _debugCursor = 0;
                    break;

                case ControlMode.Position when _colliderTarget == ColliderTarget.A:
                    _positionAControl = controlVector;
                    _debugCursor = 0;
                    break;
                case ControlMode.Position when _colliderTarget == ColliderTarget.B:
                    _positionBControl = controlVector;
                    _debugCursor = 0;
                    break;

                case ControlMode.Ratio:
                    _ratioControl = controlVector.Y * 2 / _logicalSize.Y;
                    _debugCursor = 0;
                    break;
            }

            _isCollide = Collision.Intersection(_colliderA, TransformA, _colliderB, TransformB);
            _distance = Collision.Distance(_colliderA, TransformA, _colliderB, TransformB);
            // _isCollide = Collision.Continuous(_colliderA, TransformA, _movementA.ToSystemVector2(), _colliderB,
            //     TransformB, _movementB.ToSystemVector2(), out var t, out var normal);
            // _distance = t;
            // _time = t;

            base.Update(gameTime);
        }

        private ColliderTarget DetermineTarget(Point mousePosition)
        {
            var mouseTransform = new Transform(mousePosition.ToVector2().ToSystemVector2(), 0);
            if (Collision.Intersection(_pointer, mouseTransform, _colliderA, TransformAWithCurrentMovement))
            {
                return ColliderTarget.A;
            }

            if (Collision.Intersection(_pointer, mouseTransform, _colliderB, TransformBWithCurrentMovement))
            {
                return ColliderTarget.B;
            }

            return ColliderTarget.None;
        }

        private static readonly Color ColorA = Color.Aqua;
        private static readonly Color ColorB = Color.Orange;
        private static readonly Color ColorBSubA = Color.White;
        private static readonly Color ColorCollision = Color.Yellow;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_bgColor);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            DrawUI(gameTime);

            // A
            var translationA = new SystemVector2(PositionA.X, PositionA.Y);
            var transformA = new Transform(translationA, 0);
            DrawCollider(_colliderA, transformA, _movementA, Ratio, ColorA);

            // B
            var translationB = new SystemVector2(PositionB.X, PositionB.Y);
            var transformB = new Transform(translationB, 0);
            DrawCollider(_colliderB, transformB, _movementB, Ratio, ColorB);

            DrawDebug(gameTime);

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private Texture2D _pixel;
        private ReadOnlyMemory<SystemVector2> _sampleNormals;
        private const int SampleRate = 128;

        private void DrawCollider(ICollider collider, Transform transform, Vector2 offset, Color color)
        {
            Span<Vector2> samplePoints = stackalloc Vector2[_sampleNormals.Length];

            var samples = _sampleNormals.Span;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = collider.WorldSupport(transform, samples[i]);
                var worldPosition = new Vector2(support.X, support.Y) + offset;
                samplePoints[i] = worldPosition;
            }

            DrawShapeOfSamplePoints(samplePoints, color);
        }

        private void DrawCollider(ICollider collider, Transform transform, Vector2 movement, float ratio, Color color)
        {
            var offset = movement * ratio;
            DrawCollider(collider, transform, offset, color);
            var movementStart = new Vector2(transform.Translation.X, transform.Translation.Y);
            var movementEnd = new Vector2(transform.Translation.X + movement.X, transform.Translation.Y + movement.Y);
            _spriteBatch.DrawLine(movementStart, movementEnd, color);
        }

        private void DrawMinkowskiDifference(ICollider a, Transform ta, ICollider b, Transform tb, Vector2 originAt,
            Color color)
        {
            Span<Vector2> samplePoints = stackalloc Vector2[_sampleNormals.Length];
            var samples = _sampleNormals.Span;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = a.WorldSupport(ta, samples[i]) - b.WorldSupport(tb, -samples[i]);
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
            _spriteBatch.DrawString(_defaultFont, "# Minkowski Difference A-B", stringPosition, ColorBSubA, 0,
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

            const string note = "@ ViLAWAVE.Echollision Hybrid Collision Detection";
            var noteSize = _defaultFont.MeasureString(note);
            _spriteBatch.DrawString(_defaultFont, note,
                _logicalSize.ToVector2() - noteSize * 2 - new Vector2(0, _defaultFont.LineSpacing), Color.LightGray,
                0,
                Vector2.Zero, 2, SpriteEffects.None, 0);

            // var testSize = _defaultFont.MeasureString(asciiTestString);
            // _spriteBatch.DrawString(_defaultFont, asciiTestString, _logicalSize.ToVector2() - testSize * 2,
            //     Color.LightGray, 0, Vector2.Zero, 2,
            //     SpriteEffects.None, 0);
            const int ratioBarThickness = 10;
            var lineColor = Ratio > _time ? Color.Yellow : Color.LightGray;
            var barStart = new Vector2(0, _logicalSize.Y - ratioBarThickness);
            var barEnd = new Vector2(_logicalSize.X * Ratio, _logicalSize.Y - ratioBarThickness);
            _spriteBatch.DrawLine(barStart, barEnd, lineColor, ratioBarThickness);
        }

        private Vector2 MinkowskiDifferenceOrigin => _logicalSize.ToVector2() / 2;

        private void DrawDebug(GameTime gameTime)
        {
            // B-A
            var bSubAOrigin = MinkowskiDifferenceOrigin;
            DrawMinkowskiDifference(_colliderA, TransformA, _colliderB, TransformB,
                bSubAOrigin, _isCollide ? ColorCollision : ColorBSubA);

            // Distance
            _spriteBatch.DrawString(_defaultFont, $"Distance: {_distance}", new Vector2(16, _logicalSize.Y - 36),
                Color.White, 0, Vector2.Zero, 4, SpriteEffects.None, 0);

            // Counter
            _spriteBatch.DrawString(_defaultFont, $"k: {DebugDraw.IterationCounter}", new Vector2(16, _logicalSize.Y - 68),
                Color.White, 0, Vector2.Zero, 4, SpriteEffects.None, 0);
            if (DebugDraw.IterationCounter > 60000)
            {
                System.Diagnostics.Debug.WriteLine($"PositionA: new Vector2({PositionA.X}, {PositionA.Y})");
                System.Diagnostics.Debug.WriteLine($"PositionB: new Vector2({PositionB.X}, {PositionB.Y})");
            }

            // Debug draws
            for (var i = 0; i < DebugDraw.DebugLines.Count; i += 2)
            {
                _spriteBatch.DrawLine(DebugDraw.DebugLines[i].ToXnaVector2() + bSubAOrigin,
                    DebugDraw.DebugLines[i + 1].ToXnaVector2() + bSubAOrigin, Color.Green);
            }

            for (var i = 0; i < DebugDraw.DebugPoints.Count; i += 1)
            {
                DrawCross(DebugDraw.DebugPoints[i].ToXnaVector2() + bSubAOrigin, Color.LightGreen);
            }


            for (var i = 0; i < DebugDraw.DebugStrings.Count; i += 1)
            {
                _spriteBatch.DrawString(_defaultFont, DebugDraw.DebugStrings[i].Item1,
                    DebugDraw.DebugStrings[i].Item2.ToXnaVector2() + bSubAOrigin + new Vector2(2, 2), Color.LightGreen);
            }

            if (DebugDraw.DebugSimplexes.Count == 0) return;
            var simplexIndex = Math.Clamp(_debugCursor, 0, DebugDraw.DebugSimplexes.Count - 1);
            var (simplexVertexCount, w, v, newW) = DebugDraw.DebugSimplexes[simplexIndex];
            switch (simplexVertexCount)
            {
                case 1:
                    _spriteBatch.DrawPoint(v.ToXnaVector2() + bSubAOrigin, Color.Yellow, size: 5f);
                    _spriteBatch.DrawPoint(newW.ToXnaVector2() + bSubAOrigin, Color.Red, size: 5f);
                    break;

                case 2:
                    _spriteBatch.DrawLine(w[0].ToXnaVector2() + bSubAOrigin, w[1].ToXnaVector2() + bSubAOrigin,
                        Color.Yellow);
                    _spriteBatch.DrawPoint(v.ToXnaVector2() + bSubAOrigin, Color.Yellow, size: 5f);
                    _spriteBatch.DrawPoint(newW.ToXnaVector2() + bSubAOrigin, Color.Red, size: 5f);
                    break;

                case 3:
                    _spriteBatch.DrawLine(w[0].ToXnaVector2() + bSubAOrigin, w[1].ToXnaVector2() + bSubAOrigin,
                        Color.Yellow);
                    _spriteBatch.DrawLine(w[1].ToXnaVector2() + bSubAOrigin, w[2].ToXnaVector2() + bSubAOrigin,
                        Color.Yellow);
                    _spriteBatch.DrawLine(w[2].ToXnaVector2() + bSubAOrigin, w[0].ToXnaVector2() + bSubAOrigin,
                        Color.Yellow);
                    _spriteBatch.DrawPoint(v.ToXnaVector2() + bSubAOrigin, Color.Yellow, size: 5f);
                    _spriteBatch.DrawPoint(newW.ToXnaVector2() + bSubAOrigin, Color.Red, size: 5f);
                    break;
            }
        }
    }
}