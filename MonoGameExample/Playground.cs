using System;
using System.Numerics;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
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
            var obPosition = Framework.LogicalSize.ToVector2().ToSystemVector2() / 2f;
            ob.Set(new Transform2D {Position = obPosition, Rotation = 0f});
            ob.Set(new Obstruction {Speed = 240f, AngularSpeed = 1f * MathF.PI});

            _logicalSystem = new SequentialSystem<GameTime>(
                new ActionSystem<GameTime>(FireSystem),
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
            var obTransform = obs[0].Get<Transform2D>().ToCollisionTransform();

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

                var isHit = Collision.Continuous(
                    _obstructionCollider, obTransform, Vector2.Zero,
                    _bulletCollider, transform.ToCollisionTransform(), translation,
                    out var t, out var n
                );

                bullet.Start = transform.Position;
                if (isHit)
                {
                    bullet.IsHit = true;
                    bullet.Orientation = n;
                    var hitPoint = transform.Position + translation * t;
                    bullet.Hit = hitPoint;
                    transform.Position = hitPoint + (1f - t) * Vector2.Normalize(n) * bullet.Speed *
                        (float) gameTime.ElapsedGameTime.TotalSeconds;
                }
                else
                {
                    bullet.IsHit = false;
                    transform.Position += translation;
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
                if (bullet.IsHit)
                {
                    SpriteBatch.DrawLine(bullet.Start.ToXnaVector2(), bullet.Hit.ToXnaVector2(), Color.Aqua);
                    SpriteBatch.DrawLine(bullet.Hit.ToXnaVector2(), transform.Position.ToXnaVector2(), Color.Aqua);
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