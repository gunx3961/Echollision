#define COLLISION_DEBUG

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using ViLAWAVE.Echollision;
using SystemVector2 = System.Numerics.Vector2;

namespace MonoGameExample
{
    public class NarrowPhase : Screen
    {
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

        private enum DetectMode
        {
            Intersection,
            Distance,
            Penetration,
            Continuous
        }

        private struct IntersectionResult
        {
            public bool Intersection;
        }

        private struct DistanceResult
        {
            public float Distance;
        }

        private struct PenetrationResult
        {
            public float Dpeth;
            public SystemVector2 Normal;
        }

        private struct ContinuousResult
        {
            public bool Intersection;
            public SystemVector2 Normal;
            public float Toi;
        }

        public NarrowPhase(Framework framework) : base(framework)
        {
        }

        private readonly Color _bgColor = new Color(30, 30, 30);

        private ControlMode _controlMode = ControlMode.None;
        private ColliderTarget _colliderTarget = ColliderTarget.None;
        private DetectMode _detectMode;
        private Point _controlAnchor = Point.Zero;
        private readonly Collider _pointer = new SphereCollider(0);

        private IntersectionResult _intersectionResult;
        private DistanceResult _distanceResult;
        private PenetrationResult _penetrationResult;
        private ContinuousResult _continuousResult;

        // private bool _isCollide = false;
        // private float _distance = 0f;
        // private float _time = 1f;

        private Collider _colliderA;
        private Collider _colliderB;
        private int _debugCursor = 0;

        // Position
        private Vector2 _positionABase = new Vector2(500, 469);
        private Vector2 _positionAControl = Vector2.Zero;
        private Vector2 PositionA => _positionABase + _positionAControl;

        private Vector2 _positionBBase = new Vector2(500, 250);
        private Vector2 _positionBControl = Vector2.Zero;
        private Vector2 PositionB => _positionBBase + _positionBControl;

        // Movement
        private Vector2 _movementA = new Vector2(0, 0);

        private Vector2 _movementB = new Vector2(0, 0);

        // Ratio
        private float _ratioBase = 0f;
        private float _ratioControl = 0f;
        private float Ratio => Math.Clamp(_ratioBase + _ratioControl, 0f, 1f);

        private ColliderTransform TransformA => new ColliderTransform(PositionA.ToSystemVector2(), 0);

        private ColliderTransform TransformAWithCurrentMovement =>
            new ColliderTransform((PositionA + _movementA * Ratio).ToSystemVector2(), 0);

        private ColliderTransform TransformB => new ColliderTransform(PositionB.ToSystemVector2(), 0);

        private ColliderTransform TransformBWithCurrentMovement =>
            new ColliderTransform((PositionB + _movementB * Ratio).ToSystemVector2(), 0);

        public override void Initialize()
        {
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

            _colliderA = new SphereCollider(65535);
            _colliderA = new ConvexCollider(new SystemVector2[]
            {
                new SystemVector2(-200, -100),
                new SystemVector2(200, -100),
                new SystemVector2(100, 100),
            });

            _colliderA = new ConvexHullCollider(new Collider[]
            {
                new SphereCollider(200),
                new ConvexCollider(new SystemVector2[]
                {
                    new SystemVector2(-200, -100),
                    new SystemVector2(200, -100),
                    new SystemVector2(100, 100),
                })
            });

            _colliderB = new SphereCollider(39610);
            _colliderB = new SphereCollider(0);
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
            base.LoadContent();

            _pixel = new Texture2D(SpriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixel.SetData(new[] {Color.White});
        }

        public override void Update(GameTime gameTime)
        {
            if (Framework.KeyboardState.WasKeyJustDown(Keys.Escape))
            {
                Framework.ScreenManager.LaunchMainMenu();
            }

            // Debug cursor
            if (Framework.KeyboardState.WasKeyJustDown(Keys.R)) _debugCursor = 0;

            if (Framework.KeyboardState.WasKeyJustDown(Keys.Left))
            {
                _debugCursor = Math.Clamp(_debugCursor - 1, 0, Int32.MaxValue);
            }
            else if (Framework.KeyboardState.WasKeyJustDown(Keys.Right))
            {
                _debugCursor = Math.Clamp(_debugCursor + 1, 0, Int32.MaxValue);
            }

            // Dump
            if (Framework.KeyboardState.WasKeyJustDown(Keys.T))
            {
                System.Diagnostics.Debug.WriteLine($"PositionA: new Vector2({PositionA.X}, {PositionA.Y})");
                System.Diagnostics.Debug.WriteLine($"PositionB: new Vector2({PositionB.X}, {PositionB.Y})");
            }

            // Detect mode
            if (Framework.KeyboardState.WasKeyJustDown(Keys.D1)) _detectMode = DetectMode.Intersection;
            if (Framework.KeyboardState.WasKeyJustDown(Keys.D2)) _detectMode = DetectMode.Distance;
            if (Framework.KeyboardState.WasKeyJustDown(Keys.D3)) _detectMode = DetectMode.Penetration;
            if (Framework.KeyboardState.WasKeyJustDown(Keys.D4)) _detectMode = DetectMode.Continuous;

            switch (_controlMode)
            {
                case ControlMode.None when Framework.MouseState.WasButtonJustDown(MouseButton.Left):
                    _controlMode = ControlMode.Movement;
                    _colliderTarget = DetermineTarget(Framework.MouseState.Position);
                    _controlAnchor = Framework.MouseState.Position;
                    break;
                case ControlMode.None when Framework.MouseState.WasButtonJustDown(MouseButton.Middle):
                    _controlMode = ControlMode.Position;
                    _colliderTarget = DetermineTarget(Framework.MouseState.Position);
                    _controlAnchor = Framework.MouseState.Position;
                    break;
                case ControlMode.None when Framework.MouseState.WasButtonJustDown(MouseButton.Right):
                    _controlMode = ControlMode.Ratio;
                    _controlAnchor = Framework.MouseState.Position;
                    break;

                case ControlMode.Movement when Framework.MouseState.WasButtonJustUp(MouseButton.Left):
                case ControlMode.Position when Framework.MouseState.WasButtonJustUp(MouseButton.Middle):
                case ControlMode.Ratio when Framework.MouseState.WasButtonJustUp(MouseButton.Right):
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

            var controlVector = (Framework.MouseState.Position - _controlAnchor).ToVector2();

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
                    _ratioControl = controlVector.Y * 2 / Framework.LogicalSize.Y;
                    _debugCursor = 0;
                    break;
            }


            switch (_detectMode)
            {
                case DetectMode.Intersection:
                    _intersectionResult.Intersection = Framework.Collision.Intersection(_colliderA, TransformA, _colliderB, TransformB);
                    break;

                case DetectMode.Distance:
                    _distanceResult.Distance = Framework.Collision.Distance(_colliderA, TransformA, _colliderB, TransformB);
                    break;

                case DetectMode.Penetration:
                    Framework.Collision.Penetration(_colliderA, TransformA, _colliderB, TransformB, out var pn, out var depth);
                    _penetrationResult.Normal = pn;
                    _penetrationResult.Dpeth = depth;
                    break;

                case DetectMode.Continuous:
                    _continuousResult.Intersection = Framework.Collision.Continuous(_colliderA, TransformA, _movementA.ToSystemVector2(), _colliderB,
                        TransformB, _movementB.ToSystemVector2(), out var t, out var n);
                    _continuousResult.Toi = t;
                    _continuousResult.Normal = n;
                    break;
            }
        }

        private ColliderTarget DetermineTarget(Point mousePosition)
        {
            var mouseTransform = new ColliderTransform(mousePosition.ToVector2().ToSystemVector2(), 0);
            if (Framework.Collision.Intersection(_pointer, mouseTransform, _colliderA, TransformAWithCurrentMovement))
            {
                return ColliderTarget.A;
            }

            if (Framework.Collision.Intersection(_pointer, mouseTransform, _colliderB, TransformBWithCurrentMovement))
            {
                return ColliderTarget.B;
            }

            return ColliderTarget.None;
        }

        private static readonly Color ColorA = Color.Aqua;
        private static readonly Color ColorB = Color.Orange;
        private static readonly Color ColorCso = Color.White;
        private static readonly Color ColorCollision = Color.Yellow;

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_bgColor);

            SpriteBatch.BeginPixelPerfect();

            DrawUI(gameTime);
            DrawResult(gameTime);

            SpriteBatch.End();
        }

        private Texture2D _pixel;
        private ReadOnlyMemory<SystemVector2> _sampleNormals;
        private const int SampleRate = 128;

        private void DrawCollider(Collider collider, ColliderTransform transform, Vector2 offset, Color color)
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

        private void DrawCollider(Collider collider, ColliderTransform transform, Vector2 movement, float ratio, Color color)
        {
            var offset = movement * ratio;
            DrawCollider(collider, transform, offset, color);
            var movementStart = new Vector2(transform.Translation.X, transform.Translation.Y);
            var movementEnd = new Vector2(transform.Translation.X + movement.X, transform.Translation.Y + movement.Y);
            SpriteBatch.DrawLine(movementStart, movementEnd, color);
        }

        private void DrawMinkowskiDifference(Collider a, ColliderTransform ta, Collider b, ColliderTransform tb, Vector2 originAt,
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
                SpriteBatch.Draw(_pixel, position, null, color);

                if (i > 0 && (position - prev).LengthSquared() > 100)
                {
                    SpriteBatch.DrawLine(position, prev, color, 1);
                }

                prev = position;

                if (i == 0) first = position;
                if (i == points.Length - 1) last = position;
            }

            if ((first - last).LengthSquared() > 100)
            {
                SpriteBatch.DrawLine(first, last, color, 1);
            }
        }

        private void DrawCross(Vector2 position, Color color)
        {
            SpriteBatch.Draw(_pixel, position + new Vector2(-2, -2), null, color);
            SpriteBatch.Draw(_pixel, position + new Vector2(2, -2), null, color);
            SpriteBatch.Draw(_pixel, position + new Vector2(-1, -1), null, color);
            SpriteBatch.Draw(_pixel, position + new Vector2(1, -1), null, color);
            SpriteBatch.Draw(_pixel, position, null, color);
            SpriteBatch.Draw(_pixel, position + new Vector2(-1, 1), null, color);
            SpriteBatch.Draw(_pixel, position + new Vector2(1, 1), null, color);
            SpriteBatch.Draw(_pixel, position + new Vector2(-2, 2), null, color);
            SpriteBatch.Draw(_pixel, position + new Vector2(2, 2), null, color);
        }

        private void DrawUI(GameTime gameTime)
        {
            const string asciiTestString =
                " !\"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";


            SpriteBatch.DrawString(DefaultFont, asciiTestString, Vector2.Zero, Color.LightGray, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);

            var stringPosition = new Vector2(0, DefaultFont.LineSpacing * 4);
            var mode = _detectMode switch
            {
                DetectMode.Intersection => "INTERSECTION",
                DetectMode.Distance => "DISTANCE",
                DetectMode.Penetration => "PENETRATION",
                DetectMode.Continuous => "CONTINUOUS"
            };

            SpriteBatch.DrawString(DefaultFont, mode, stringPosition, Color.White, 0, Vector2.Zero, 4, SpriteEffects.None, 0);
            stringPosition.Y += DefaultFont.LineSpacing * 6;

            SpriteBatch.DrawString(DefaultFont, "1, 2, 3, 4: Mode Selection", stringPosition, Color.White, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);
            stringPosition.Y += DefaultFont.LineSpacing * 2;

            SpriteBatch.DrawString(DefaultFont, "# Shape A", stringPosition, ColorA, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
            stringPosition.Y += DefaultFont.LineSpacing * 2;

            SpriteBatch.DrawString(DefaultFont, "# Shape B", stringPosition, ColorB, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
            stringPosition.Y += DefaultFont.LineSpacing * 2;

            SpriteBatch.DrawString(DefaultFont, "# CSO", stringPosition, ColorCso, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
            stringPosition.Y += DefaultFont.LineSpacing * 2;

            var fps = Math.Ceiling(1.0 / gameTime.ElapsedGameTime.TotalSeconds);
            var fpsText = $"FPS:{fps.ToString()}";
            stringPosition = new Vector2(Framework.LogicalSize.X, DefaultFont.LineSpacing * 6) -
                             DefaultFont.MeasureString(fpsText) * 2f;
            SpriteBatch.DrawString(DefaultFont, fpsText, stringPosition, Color.LightGray, 0, Vector2.Zero, 2,
                SpriteEffects.None, 0);

            const string note = "@ ViLAWAVE.Echollision Hybrid Collision Detection";
            var noteSize = DefaultFont.MeasureString(note);
            SpriteBatch.DrawString(DefaultFont, note,
                Framework.LogicalSize.ToVector2() - noteSize * 2 - new Vector2(0, DefaultFont.LineSpacing),
                Color.LightGray,
                0,
                Vector2.Zero, 2, SpriteEffects.None, 0);
        }

        private Vector2 DebugOrigin => Framework.LogicalSize.ToVector2() / 2;

        private void DrawResult(GameTime gameTime)
        {
            // A
            var translationA = new SystemVector2(PositionA.X, PositionA.Y);
            var transformA = new ColliderTransform(translationA, 0);
            DrawCollider(_colliderA, transformA, _movementA, Ratio, ColorA);

            // B
            var translationB = new SystemVector2(PositionB.X, PositionB.Y);
            var transformB = new ColliderTransform(translationB, 0);
            DrawCollider(_colliderB, transformB, _movementB, Ratio, ColorB);

            // CSO
            var debugOrigin = DebugOrigin;
            switch (_detectMode)
            {
                // MPR: B-A
                case DetectMode.Intersection:
                    DrawMinkowskiDifference(_colliderB, TransformB, _colliderA, TransformA,
                        debugOrigin, _intersectionResult.Intersection ? ColorCollision : ColorCso);
                    break;
                case DetectMode.Penetration:
                    DrawMinkowskiDifference(_colliderB, TransformB, _colliderA, TransformA,
                        debugOrigin, _penetrationResult.Dpeth >= 0 ? ColorCollision : ColorCso);
                    break;

                // GJK: A-B
                case DetectMode.Distance:
                    DrawMinkowskiDifference(_colliderA, TransformA, _colliderB, TransformB,
                        debugOrigin, _distanceResult.Distance <= 0 ? ColorCollision : ColorCso);
                    break;
                case DetectMode.Continuous:
                    DrawMinkowskiDifference(_colliderA, TransformA, _colliderB, TransformB,
                        debugOrigin, _continuousResult.Intersection ? ColorCollision : ColorCso);
                    break;
            }


            // GJK procedures
            if (DebugDraw.GjkProcedure.Count > 0)
            {
                var procedureIndex = Math.Clamp(_debugCursor, 0, DebugDraw.GjkProcedure.Count - 1);
                var (simplexVertexCount, w, v, newW) = DebugDraw.GjkProcedure[procedureIndex];
                switch (simplexVertexCount)
                {
                    case 1:
                        SpriteBatch.DrawPoint(v.ToXnaVector2() + debugOrigin, Color.Yellow, size: 5f);
                        SpriteBatch.DrawPoint(newW.ToXnaVector2() + debugOrigin, Color.Red, size: 5f);
                        break;

                    case 2:
                        SpriteBatch.DrawLine(w[0].ToXnaVector2() + debugOrigin, w[1].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawPoint(v.ToXnaVector2() + debugOrigin, Color.Yellow, size: 5f);
                        SpriteBatch.DrawPoint(newW.ToXnaVector2() + debugOrigin, Color.Red, size: 5f);
                        break;

                    case 3:
                        SpriteBatch.DrawLine(w[0].ToXnaVector2() + debugOrigin, w[1].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawLine(w[1].ToXnaVector2() + debugOrigin, w[2].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawLine(w[2].ToXnaVector2() + debugOrigin, w[0].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawPoint(v.ToXnaVector2() + debugOrigin, Color.Yellow, size: 5f);
                        SpriteBatch.DrawPoint(newW.ToXnaVector2() + debugOrigin, Color.Red, size: 5f);
                        break;
                }
            }

            // GJK Ray Cast procedures
            if (DebugDraw.GjkRayCastProcedure.Count > 0)
            {
                var procedureIndex = Math.Clamp(_debugCursor, 0, DebugDraw.GjkRayCastProcedure.Count - 1);
                var (x, p, vertexCount, setX, v) = DebugDraw.GjkRayCastProcedure[procedureIndex];
                SpriteBatch.DrawString(DefaultFont, "x", x.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "p", p.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawLine(x.ToXnaVector2() + debugOrigin, p.ToXnaVector2() + debugOrigin, Color.Yellow);

                var vInCsoSystem = (x - v).ToXnaVector2();
                SpriteBatch.DrawString(DefaultFont, "v", vInCsoSystem + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawLine(x.ToXnaVector2() + debugOrigin, vInCsoSystem + debugOrigin, Color.MediumPurple);

                switch (vertexCount)
                {
                    case 2:
                        SpriteBatch.DrawLine(setX[0].ToXnaVector2() + debugOrigin,
                            setX[1].ToXnaVector2() + debugOrigin,
                            Color.White);
                        break;

                    case 3:
                        SpriteBatch.DrawLine(setX[0].ToXnaVector2() + debugOrigin,
                            setX[1].ToXnaVector2() + debugOrigin,
                            Color.White);
                        SpriteBatch.DrawLine(setX[1].ToXnaVector2() + debugOrigin,
                            setX[2].ToXnaVector2() + debugOrigin,
                            Color.White);
                        SpriteBatch.DrawLine(setX[2].ToXnaVector2() + debugOrigin,
                            setX[0].ToXnaVector2() + debugOrigin,
                            Color.White);
                        break;
                }
            }

            // MPR procedures
            if (DebugDraw.MprProcedure.Count > 0)
            {
                var procedureIndex = Math.Clamp(_debugCursor, 0, DebugDraw.MprProcedure.Count - 1);
                var (v0, v1, v2, v3) = DebugDraw.MprProcedure[procedureIndex];

                SpriteBatch.DrawPoint(v0.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawPoint(v1.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawPoint(v2.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawPoint(v3.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawString(DefaultFont, "v0", v0.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "v1", v1.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "v2", v2.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "v3", v3.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);

                SpriteBatch.DrawLine(v0.ToXnaVector2() + debugOrigin, v1.ToXnaVector2() + debugOrigin, Color.Yellow);
                SpriteBatch.DrawLine(v0.ToXnaVector2() + debugOrigin, v2.ToXnaVector2() + debugOrigin, Color.Yellow);
                SpriteBatch.DrawLine(v1.ToXnaVector2() + debugOrigin, v2.ToXnaVector2() + debugOrigin, Color.Purple);
            }

            // Penetration
            if (_detectMode == DetectMode.Penetration)
            {
                SpriteBatch.DrawPoint(DebugDraw.PenetrationA.ToXnaVector2(), ColorA, size: 4f);
                SpriteBatch.DrawPoint(DebugDraw.PenetrationB.ToXnaVector2(), ColorB, size: 4f);
                var normalizedNormal = SystemVector2.Normalize(DebugDraw.PenetrationNormal);
                var planeStart = new Vector2(-normalizedNormal.Y, normalizedNormal.X) * 300;
                var planeEnd = -planeStart;

                SpriteBatch.DrawLine(planeStart + DebugDraw.PenetrationA.ToXnaVector2(),
                    planeEnd + DebugDraw.PenetrationA.ToXnaVector2(), Color.LightPink);
                SpriteBatch.DrawLine(planeStart + DebugDraw.PenetrationB.ToXnaVector2(),
                    planeEnd + DebugDraw.PenetrationB.ToXnaVector2(), Color.LightPink);
            }

            if (_detectMode == DetectMode.Continuous)
            {
                const int ratioBarThickness = 10;
                var lineColor = Ratio > _continuousResult.Toi ? Color.Yellow : Color.LightGray;
                var barStart = new Vector2(0, Framework.LogicalSize.Y - ratioBarThickness);
                var barEnd = new Vector2(Framework.LogicalSize.X * Ratio, Framework.LogicalSize.Y - ratioBarThickness);
                SpriteBatch.DrawLine(barStart, barEnd, lineColor, ratioBarThickness);
            }
        }

        private void DrawDebug(GameTime gameTime)
        {
            // B-A
            var debugOrigin = DebugOrigin;
            // DrawMinkowskiDifference(_colliderA, TransformA, _colliderB, TransformB,
            //     debugOrigin, _isCollide ? ColorCollision : ColorBSubA);
            // DrawMinkowskiDifference(_colliderB, TransformB, _colliderA, TransformA,
            //     debugOrigin, _isCollide ? ColorCollision : ColorCso);
            //
            // // Distance
            // SpriteBatch.DrawString(DefaultFont, $"Distance: {_distance.ToString()}",
            //     new Vector2(16, Framework.LogicalSize.Y - 36),
            //     Color.White, 0, Vector2.Zero, 4, SpriteEffects.None, 0);

            // Counter
            SpriteBatch.DrawString(DefaultFont, $"k: {DebugDraw.IterationCounter.ToString()}",
                new Vector2(16, Framework.LogicalSize.Y - 68),
                Color.White, 0, Vector2.Zero, 4, SpriteEffects.None, 0);
            if (DebugDraw.IterationCounter > 60000)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"PositionA: new Vector2({PositionA.X.ToString()}, {PositionA.Y.ToString()})");
                System.Diagnostics.Debug.WriteLine(
                    $"PositionB: new Vector2({PositionB.X.ToString()}, {PositionB.Y.ToString()})");
            }

            // Debug draws
            for (var i = 0; i < DebugDraw.DebugLines.Count; i += 2)
            {
                SpriteBatch.DrawLine(DebugDraw.DebugLines[i].ToXnaVector2() + debugOrigin,
                    DebugDraw.DebugLines[i + 1].ToXnaVector2() + debugOrigin, Color.Green);
            }

            for (var i = 0; i < DebugDraw.DebugPoints.Count; i += 1)
            {
                DrawCross(DebugDraw.DebugPoints[i].ToXnaVector2() + debugOrigin, Color.LightGreen);
            }


            for (var i = 0; i < DebugDraw.DebugStrings.Count; i += 1)
            {
                SpriteBatch.DrawString(DefaultFont, DebugDraw.DebugStrings[i].Item1,
                    DebugDraw.DebugStrings[i].Item2.ToXnaVector2() + debugOrigin + new Vector2(2, 2), Color.LightGreen);
            }

            // GJK procedures
            if (DebugDraw.GjkProcedure.Count > 0)
            {
                var procedureIndex = Math.Clamp(_debugCursor, 0, DebugDraw.GjkProcedure.Count - 1);
                var (simplexVertexCount, w, v, newW) = DebugDraw.GjkProcedure[procedureIndex];
                switch (simplexVertexCount)
                {
                    case 1:
                        SpriteBatch.DrawPoint(v.ToXnaVector2() + debugOrigin, Color.Yellow, size: 5f);
                        SpriteBatch.DrawPoint(newW.ToXnaVector2() + debugOrigin, Color.Red, size: 5f);
                        break;

                    case 2:
                        SpriteBatch.DrawLine(w[0].ToXnaVector2() + debugOrigin, w[1].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawPoint(v.ToXnaVector2() + debugOrigin, Color.Yellow, size: 5f);
                        SpriteBatch.DrawPoint(newW.ToXnaVector2() + debugOrigin, Color.Red, size: 5f);
                        break;

                    case 3:
                        SpriteBatch.DrawLine(w[0].ToXnaVector2() + debugOrigin, w[1].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawLine(w[1].ToXnaVector2() + debugOrigin, w[2].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawLine(w[2].ToXnaVector2() + debugOrigin, w[0].ToXnaVector2() + debugOrigin,
                            Color.Yellow);
                        SpriteBatch.DrawPoint(v.ToXnaVector2() + debugOrigin, Color.Yellow, size: 5f);
                        SpriteBatch.DrawPoint(newW.ToXnaVector2() + debugOrigin, Color.Red, size: 5f);
                        break;
                }
            }

            // GJK Ray Cast procedures
            if (DebugDraw.GjkRayCastProcedure.Count > 0)
            {
                var procedureIndex = Math.Clamp(_debugCursor, 0, DebugDraw.GjkRayCastProcedure.Count - 1);
                var (x, p, vertexCount, setX, v) = DebugDraw.GjkRayCastProcedure[procedureIndex];
                SpriteBatch.DrawString(DefaultFont, "x", x.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "p", p.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawLine(x.ToXnaVector2() + debugOrigin, p.ToXnaVector2() + debugOrigin, Color.Yellow);

                var vInCsoSystem = (x - v).ToXnaVector2();
                SpriteBatch.DrawString(DefaultFont, "v", vInCsoSystem + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawLine(x.ToXnaVector2() + debugOrigin, vInCsoSystem + debugOrigin, Color.MediumPurple);

                switch (vertexCount)
                {
                    case 2:
                        SpriteBatch.DrawLine(setX[0].ToXnaVector2() + debugOrigin,
                            setX[1].ToXnaVector2() + debugOrigin,
                            Color.White);
                        break;

                    case 3:
                        SpriteBatch.DrawLine(setX[0].ToXnaVector2() + debugOrigin,
                            setX[1].ToXnaVector2() + debugOrigin,
                            Color.White);
                        SpriteBatch.DrawLine(setX[1].ToXnaVector2() + debugOrigin,
                            setX[2].ToXnaVector2() + debugOrigin,
                            Color.White);
                        SpriteBatch.DrawLine(setX[2].ToXnaVector2() + debugOrigin,
                            setX[0].ToXnaVector2() + debugOrigin,
                            Color.White);
                        break;
                }
            }

            // MPR procedures
            if (DebugDraw.MprProcedure.Count > 0)
            {
                var procedureIndex = Math.Clamp(_debugCursor, 0, DebugDraw.MprProcedure.Count - 1);
                var (v0, v1, v2, v3) = DebugDraw.MprProcedure[procedureIndex];

                SpriteBatch.DrawPoint(v0.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawPoint(v1.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawPoint(v2.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawPoint(v3.ToXnaVector2() + debugOrigin, Color.Yellow, size: 3f);
                SpriteBatch.DrawString(DefaultFont, "v0", v0.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "v1", v1.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "v2", v2.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                SpriteBatch.DrawString(DefaultFont, "v3", v3.ToXnaVector2() + debugOrigin + new Vector2(2, 2),
                    Color.LightGreen, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);

                SpriteBatch.DrawLine(v0.ToXnaVector2() + debugOrigin, v1.ToXnaVector2() + debugOrigin, Color.Yellow);
                SpriteBatch.DrawLine(v0.ToXnaVector2() + debugOrigin, v2.ToXnaVector2() + debugOrigin, Color.Yellow);
                SpriteBatch.DrawLine(v1.ToXnaVector2() + debugOrigin, v2.ToXnaVector2() + debugOrigin, Color.Purple);
            }

            // Penetration
            SpriteBatch.DrawPoint(DebugDraw.PenetrationA.ToXnaVector2(), ColorA, size: 4f);
            SpriteBatch.DrawPoint(DebugDraw.PenetrationB.ToXnaVector2(), ColorB, size: 4f);
            var normalizedNormal = SystemVector2.Normalize(DebugDraw.PenetrationNormal);
            var planeStart = new Vector2(-normalizedNormal.Y, normalizedNormal.X) * 300;
            var planeEnd = -planeStart;

            SpriteBatch.DrawLine(planeStart + DebugDraw.PenetrationA.ToXnaVector2(),
                planeEnd + DebugDraw.PenetrationA.ToXnaVector2(), Color.LightPink);
            SpriteBatch.DrawLine(planeStart + DebugDraw.PenetrationB.ToXnaVector2(),
                planeEnd + DebugDraw.PenetrationB.ToXnaVector2(), Color.LightPink);
        }
    }
}
