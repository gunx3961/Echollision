using System;
using System.Numerics;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using MonoGameExample.Ecs;
using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.Collider;
using Vector2 = System.Numerics.Vector2;

namespace MonoGameExample
{
    public class Playground : Screen
    {
        public Playground(Framework framework) : base(framework)
        {
        }

        private ISystem<GameTime> _logicalSystem;
        private ISystem<GameTime> _renderSystem;

        private EntitySet _obstructionSet;
        private EntitySet _bulletSet;

        private SphereCollider _bulletCollider;
        private Vector2[] _obstructionVerts;
        private ConvexCollider _obstructionCollider;

        private Random _randomSequence = new Random();

        public override void Initialize()
        {
            base.Initialize();

            _obstructionSet = World.GetEntities().With<Obstruction>().With<Transform2D>().AsSet();
            _bulletSet = World.GetEntities().With<Bullet>().With<Transform2D>().AsSet();

            _bulletCollider = new SphereCollider(0f);
            _obstructionVerts = new[]
            {
                new System.Numerics.Vector2(-160, -160),
                new System.Numerics.Vector2(160, -160),
                new System.Numerics.Vector2(160, 160),
                new System.Numerics.Vector2(-160, 160)
            };
            _obstructionCollider = new ConvexCollider(_obstructionVerts);

            // Obstructions
            var ob = World.CreateEntity();
            var obPosition = new Vector2(300, 700);
            ob.Set(new Transform2D {Position = obPosition, Rotation = 0f});
            ob.Set(new Obstruction {Speed = 240f, AngularSpeed = 1.1f * MathF.PI});

            ob = World.CreateEntity();
            obPosition = new Vector2(860, 650);
            ob.Set(new Transform2D {Position = obPosition, Rotation = 0f});
            ob.Set(new Obstruction {Speed = 240f, AngularSpeed = 0.55f * MathF.PI});

            ob = World.CreateEntity();
            obPosition = new Vector2(1200, 240);
            ob.Set(new Transform2D {Position = obPosition, Rotation = 0f});
            ob.Set(new Obstruction {Speed = 240f, AngularSpeed = 1.4f * MathF.PI});

            _logicalSystem = new SequentialSystem<GameTime>(
                // new ActionSystem<GameTime>(FireSystem),
                new ActionSystem<GameTime>(AutoFire),
                new ActionSystem<GameTime>(ObstructionSystem),
                new ActionSystem<GameTime>(BulletSystem)
            );

            _renderSystem = new SequentialSystem<GameTime>(
                new ActionSystem<GameTime>(DrawSystem)
            );
        }

        public override void Update(GameTime gameTime)
        {
            _logicalSystem.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            SpriteBatch.BeginPixelPerfect();

            _renderSystem.Update(gameTime);

            SpriteBatch.End();
        }

        private double _fireInterval = 0.1;
        private TimeSpan _lastFire;

        private void FireSystem(GameTime gameTime)
        {
            if (!Framework.MouseState.IsButtonDown(MouseButton.Left) ||
                !(gameTime.TotalGameTime.TotalSeconds - _lastFire.TotalSeconds > _fireInterval)) return;
            Fire(gameTime);
            _lastFire = gameTime.TotalGameTime;
        }

        private void Fire(GameTime gameTime)
        {
            var mousePosition = Framework.MouseState.Position.ToVector2().ToSystemVector2();
            var orientation = System.Numerics.Vector2.Normalize(mousePosition);

            var bulletEntity = World.CreateEntity();

            var initialTransform = new Transform2D {Position = System.Numerics.Vector2.Zero, Rotation = 0f};
            bulletEntity.Set(initialTransform);

            var bullet = new Bullet
            {
                Orientation = orientation,
                Speed = 5000f,
                BirthTime = gameTime.TotalGameTime,
                LifeTime = 1f
            };
            bulletEntity.Set(bullet);
        }

        private void AutoFire(GameTime gameTime)
        {
            var x = _randomSequence.NextSingle(0f, 1f);
            // var x = (float)gameTime.TotalGameTime.TotalSeconds % 1f;
            var y = 1f - x;
            var orientation = new Vector2(x, y);

            var bulletEntity = World.CreateEntity();

            var initialTransform = new Transform2D {Position = System.Numerics.Vector2.Zero, Rotation = 0f};
            bulletEntity.Set(initialTransform);

            var bullet = new Bullet
            {
                Orientation = orientation,
                Speed = 5000f,
                BirthTime = gameTime.TotalGameTime,
                LifeTime = 1f
            };
            bulletEntity.Set(bullet);
        }

        private void ObstructionSystem(GameTime gameTime)
        {
            var obs = _obstructionSet.GetEntities();
            for (var i = 0; i < obs.Length; i += 1)
            {
                ref var obstruction = ref obs[i].Get<Obstruction>();
                ref var transform = ref obs[i].Get<Transform2D>();
                transform.Rotation += (float) (gameTime.ElapsedGameTime.TotalSeconds * obstruction.AngularSpeed);
                // var offset = new Vector2(
                //     (float) (obstruction.Speed * Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5)), 0);
                // transform.Position = Framework.LogicalSize.ToVector2().ToSystemVector2() / 2f + offset;
            }
        }

        private void BulletSystem(GameTime gameTime)
        {
            var bullets = _bulletSet.GetEntities();
            var obs = _obstructionSet.GetEntities();

            for (var i = 0; i < bullets.Length; i += 1)
            {
                ref var bullet = ref bullets[i].Get<Bullet>();
                ref var transform = ref bullets[i].Get<Transform2D>();
                if (gameTime.TotalGameTime.TotalSeconds - bullet.BirthTime.TotalSeconds > bullet.LifeTime)
                {
                    bullets[i].Dispose();
                    continue;
                }

                var translation = Vector2.Normalize(bullet.Orientation) * bullet.Speed *
                                  (float) gameTime.ElapsedGameTime.TotalSeconds;

                var isHit = false;
                var t = float.MaxValue;
                var hitNormal = Vector2.One;
                var penetrationNormal = Vector2.One;

                for (var o = 0; o < obs.Length; o += 1)
                {
                    var obTransform = obs[o].Get<Transform2D>().ToCollisionTransform();
                    var tempHit = Collision.Continuous(
                        _obstructionCollider, obTransform, Vector2.Zero,
                        _bulletCollider, transform.ToCollisionTransform(), translation,
                        out var tempT, out var tempN
                    );
                    if (!(tempHit && tempT < t)) continue;
                    isHit = true;
                    t = tempT;
                    // Already contact, resolve penetration
                    if (tempT == 0f)
                    {
                        Collision.PenetrationDepth(_bulletCollider, transform.ToCollisionTransform(),
                            _obstructionCollider, obTransform, out penetrationNormal, out var d);
                    }

                    hitNormal = tempN;
                }

                bullet.Start = transform.Position;
                if (!isHit)
                {
                    bullet.IsTurning = false;
                    transform.Position += translation;
                }
                // Contact in future
                else if (t > 0)
                {
                    bullet.IsTurning = true;
                    var hitPoint = transform.Position + translation * t;
                    bullet.TurnPoint = hitPoint;

                    var reflection = Vector2.Reflect(hitPoint - transform.Position, Vector2.Normalize(hitNormal));
                    bullet.Orientation = reflection;
                    transform.Position = hitPoint + (1f - t) * Vector2.Normalize(reflection) * bullet.Speed *
                        (float) gameTime.ElapsedGameTime.TotalSeconds;
                }
                // Already contact, resolve penetration
                else
                {
                    // Simple resolving
                    bullet.IsTurning = false;
                    bullet.Orientation = Vector2.Normalize(penetrationNormal);
                    transform.Position += bullet.Orientation * bullet.Speed *
                                          (float) gameTime.ElapsedGameTime.TotalSeconds;
                }
            }
        }

        private void DrawSystem(GameTime gameTime)
        {
            var bullets = _bulletSet.GetEntities();
            var obs = _obstructionSet.GetEntities();

            Span<Vector2> verts = stackalloc Vector2[4];
            int i;
            for (i = 0; i < obs.Length; i += 1)
            {
                ref var obstruction = ref obs[i].Get<Obstruction>();
                ref var transform = ref obs[i].Get<Transform2D>();

                var rotation = Matrix3x2.CreateRotation(transform.Rotation);
                for (var j = 0; j < 4; j += 1)
                {
                    verts[j] = Vector2.Transform(_obstructionVerts[j], rotation) + transform.Position;
                }

                SpriteBatch.DrawLine(verts[0].ToXnaVector2(), verts[1].ToXnaVector2(), Color.White);
                SpriteBatch.DrawLine(verts[1].ToXnaVector2(), verts[2].ToXnaVector2(), Color.White);
                SpriteBatch.DrawLine(verts[2].ToXnaVector2(), verts[3].ToXnaVector2(), Color.White);
                SpriteBatch.DrawLine(verts[3].ToXnaVector2(), verts[0].ToXnaVector2(), Color.White);
            }

            for (i = 0; i < bullets.Length; i += 1)
            {
                ref var bullet = ref bullets[i].Get<Bullet>();
                ref var transform = ref bullets[i].Get<Transform2D>();
                if (bullet.IsTurning)
                {
                    SpriteBatch.DrawLine(bullet.Start.ToXnaVector2(), bullet.TurnPoint.ToXnaVector2(), Color.Aqua);
                    SpriteBatch.DrawLine(bullet.TurnPoint.ToXnaVector2(), transform.Position.ToXnaVector2(),
                        Color.Aqua);
                }
                else
                {
                    SpriteBatch.DrawLine(bullet.Start.ToXnaVector2(), transform.Position.ToXnaVector2(), Color.Aqua);
                }
            }
        }

        private struct Obstruction
        {
            public float Speed;
            public float AngularSpeed;
        }
    }
}