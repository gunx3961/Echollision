using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        private Point _logicalSize = new Point(1680, 1000);
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
        private ICollider _colliderA;
        private ICollider _colliderB;

        // Position
        private Vector2 _positionABase = new Vector2(400, 320);
        private Vector2 _positionAControl = Vector2.Zero;
        private Vector2 PositionA => _positionABase + _positionAControl;

        private Vector2 _positionBBase = new Vector2(420, 250);
        private Vector2 _positionBControl = Vector2.Zero;
        private Vector2 PositionB => _positionBBase + _positionBControl;

        // Movement
        private Vector2 _movementA = new Vector2(100, 100);

        private Vector2 _movementB = new Vector2(-160, 80);

        // Ratio
        private float _ratioBase = 0f;
        private float _ratioControl = 0f;
        private float Ratio => Math.Clamp(_ratioBase + _ratioControl, 0f, 1f);

        private Transform TransformA => new Transform(PositionA.ToSystemVector2(), 0);
        private Transform TransformB => new Transform(PositionB.ToSystemVector2(), 0);


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

            _colliderA = new SphereCollider(100);
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
            DebugDraw.DrawMovement += HandleDrawDebugMovement;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

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
                    break;
                case ControlMode.Movement when _colliderTarget == ColliderTarget.B:
                    _movementB = controlVector;
                    break;

                case ControlMode.Position when _colliderTarget == ColliderTarget.A:
                    _positionAControl = controlVector;
                    break;
                case ControlMode.Position when _colliderTarget == ColliderTarget.B:
                    _positionBControl = controlVector;
                    break;

                case ControlMode.Ratio:
                    _ratioControl = controlVector.Y * 2 / _logicalSize.Y;
                    break;
            }

            _isCollide = Collision.DetectPriori(_colliderA, TransformA, _movementA.ToSystemVector2(), _colliderB,
                TransformB, _movementB.ToSystemVector2());

            base.Update(gameTime);
        }

        private ColliderTarget DetermineTarget(Point mousePosition)
        {
            var mouseTransform = new Transform(mousePosition.ToVector2().ToSystemVector2(), 0);
            if (Collision.Detect(_pointer, mouseTransform, _colliderA, TransformA))
            {
                return ColliderTarget.A;
            }

            if (Collision.Detect(_pointer, mouseTransform, _colliderB, TransformB))
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

            // TODO: Add your drawing code here
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

            // B-A
            var bSubAOrigin = _logicalSize.ToVector2() / 2;
            DrawMinkowskiDifference(_colliderA, transformA, _colliderB, transformB,
                (_movementA - _movementB).ToSystemVector2(),
                bSubAOrigin, _isCollide ? ColorCollision : ColorBSubA);

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
                var support = Collision.SupportOfMinkowskiDifference(a, ta, b, tb, samples[i]);
                var worldPosition = new Vector2(support.X, support.Y) + originAt;
                samplePoints[i] = worldPosition;
            }

            DrawShapeOfSamplePoints(samplePoints, color);
        }

        private void DrawMinkowskiDifference(ICollider a, Transform ta, ICollider b, Transform tb,
            SystemVector2 relativeMovement, Vector2 originAt, Color color)
        {
            Span<Vector2> samplePoints = stackalloc Vector2[_sampleNormals.Length];
            var samples = _sampleNormals.Span;
            for (var i = 0; i < samples.Length; i += 1)
            {
                var support = Collision.SupportOfMinkowskiDifference(a, ta, b, tb, relativeMovement, samples[i]);
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

            // var testSize = _defaultFont.MeasureString(asciiTestString);
            // _spriteBatch.DrawString(_defaultFont, asciiTestString, _logicalSize.ToVector2() - testSize * 2,
            //     Color.LightGray, 0, Vector2.Zero, 2,
            //     SpriteEffects.None, 0);
            const int ratioBarThickness = 10;
            var barStart = new Vector2(0, _logicalSize.Y - ratioBarThickness);
            var barEnd = new Vector2(_logicalSize.X * Ratio, _logicalSize.Y - ratioBarThickness);
            _spriteBatch.DrawLine(barStart, barEnd, Color.LightGray, ratioBarThickness);
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

        private void HandleDrawDebugMovement(SystemVector2 start, SystemVector2 end)
        {
            var ratioPoint = start + (end - start) * Ratio;
            _debugLines.Add(new Vector2(start.X, start.Y));
            _debugLines.Add(new Vector2(end.X, end.Y));
            _debugPoints.Add(ratioPoint.ToXnaVector2());
        }
    }
}