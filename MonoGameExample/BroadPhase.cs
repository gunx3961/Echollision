using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using MonoGameExample.Ecs;
using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.BroadPhase;

namespace MonoGameExample
{
    public class BroadPhase : Screen
    {
        public BroadPhase(Framework framework) : base(framework)
        {
        }

        private List<Collider> _colliders;
        private Random _rs;

        private ISystem<GameTime> _logicalSystem;
        private ISystem<GameTime> _renderSystem;

        private EntitySet _lifeSet;
        private EntitySet _rigidBodySet;
        private EntitySet _hittableSet;

        public override void Initialize()
        {
            base.Initialize();

            _rs = new Random();
            _colliders = new List<Collider>();
            // _colliders.Add(new SphereCollider(0f));
            _colliders.Add(new SphereCollider(32f));
            _colliders.Add(new SphereCollider(32f));
            _colliders.Add(new SphereCollider(32f));
            foreach (var c in _colliders)
            {
                c.InitializeBounding();
            }

            // Walls
            var (x, y) = Framework.LogicalSize;
            var xBox = ColliderFactory.Rect(x, x);
            xBox.InitializeBounding();
            var yBox = ColliderFactory.Rect(new Vector2(y));
            yBox.InitializeBounding();

            var left = World.CreateEntity();
            left.Set(new Transform(new System.Numerics.Vector2(-y * 0.5f, y * 0.5f)));
            left.Set(new Hittable {Collider = yBox});

            var right = World.CreateEntity();
            right.Set(new Transform(new System.Numerics.Vector2(x + y * 0.5f, y * 0.5f)));
            right.Set(new Hittable {Collider = yBox});

            var ground = World.CreateEntity();
            ground.Set(new Transform(new System.Numerics.Vector2(x * 0.5f, y + x * 0.5f)));
            ground.Set(new Hittable {Collider = xBox});

            _lifeSet = World.GetEntities().With<Life>().AsSet();
            _rigidBodySet = World.GetEntities().With<RigidBody>().AsSet();
            _hittableSet = World.GetEntities().With<Hittable>().AsSet();

            _logicalSystem = new SequentialSystem<GameTime>(
                new ActionSystem<GameTime>(LifeSystem),
                new ActionSystem<GameTime>(SpawnSystem),
                new ActionSystem<GameTime>(RehearsalSystem),
                new ActionSystem<GameTime>(CollisionResolveSystem),
                new ActionSystem<GameTime>(DestSystem)
            );

            _renderSystem = new ActionSystem<GameTime>(DrawSystem);
        }

        private float _lastSpawnTime;
        private const float SpawnInterval = 0.1f;

        private void SpawnSystem(GameTime gameTime)
        {
            var currentTime = (float) gameTime.TotalGameTime.TotalSeconds;
            if (Framework.MouseState.IsButtonUp(MouseButton.Left) || currentTime < _lastSpawnTime + SpawnInterval) return;

            _lastSpawnTime = currentTime;
            var randomCollider = _colliders[_rs.Next(0, _colliders.Count)];
            var e = World.CreateEntity();
            e.Set(new Life {BirthTime = (float) gameTime.TotalGameTime.TotalSeconds, Length = 5f});
            e.Set(new Hittable {Collider = randomCollider});

            _rs.NextUnitVector(out var randomDirection);
            var randomSpeed = _rs.NextSingle(100, 2000);
            var vel = randomDirection.ToSystemVector2() * randomSpeed;
            e.Set(new RigidBody {Velocity = vel, AngularVelocity = 0f});

            var spawnPosition = Framework.MouseState.Position.ToVector2().ToSystemVector2();
            var randomRotation = _rs.NextAngle();
            e.Set(new Transform(spawnPosition, randomRotation));
        }

        private void LifeSystem(GameTime gameTime)
        {
            var es = _lifeSet.GetEntities();
            for (var i = 0; i < es.Length; i += 1)
            {
                ref var life = ref es[i].Get<Life>();
                if ((float) gameTime.TotalGameTime.TotalSeconds > life.DeathTime) es[i].Dispose();
            }
        }

        private void RehearsalSystem(GameTime gameTime)
        {
            var es = _rigidBodySet.GetEntities();
            for (var i = 0; i < es.Length; i += 1)
            {
                ref var rigidBody = ref es[i].Get<RigidBody>();
                ref var transform = ref es[i].Get<Transform>();
                transform.DestinationPosition = transform.Position + (float) gameTime.ElapsedGameTime.TotalSeconds * rigidBody.Velocity;
                transform.DestinationRotation = transform.Rotation + (float) gameTime.ElapsedGameTime.TotalSeconds * rigidBody.AngularVelocity;
            }
        }

        private void CollisionResolveSystem(GameTime gameTime)
        {
            var es = _hittableSet.GetEntities();
            
            Span<SweptBox> boxes = stackalloc SweptBox[es.Length];
            for (var i = 0; i < es.Length; i += 1)
            {
                ref var transform = ref es[i].Get<Transform>();
                ref var hittable = ref es[i].Get<Hittable>();
                var colliderTransform = transform.ToColliderTransform();
                var movement = transform.DestinationPosition - transform.Position;
                var box = hittable.Collider.SweepBox(colliderTransform, movement);
                box.Id = es[i].GetHashCode();
                hittable.SweptBox = box;
                boxes[i] = box;
            }

            int CompareX(SweptBox a, SweptBox b) => Math.Sign(a.From.X - b.From.X);
            int CompareY(SweptBox a, SweptBox b) => Math.Sign(a.From.Y - b.From.Y);

            boxes.Sort(CompareX);
            boxes.Sort(CompareY);
        }

        private void DestSystem(GameTime gameTime)
        {
            var es = _rigidBodySet.GetEntities();
            for (var i = 0; i < es.Length; i += 1)
            {
                ref var transform = ref es[i].Get<Transform>();
                transform.ApplyDestination();
            }
        }

        private void DrawSystem(GameTime gameTime)
        {
            var es = _rigidBodySet.GetEntities();
            for (var i = 0; i < es.Length; i += 1)
            {
                ref var transform = ref es[i].Get<Transform>();
                SpriteBatch.DrawCircle(transform.Position.ToXnaVector2(), 32f, 16, Color.White);
                
                if (es[i].Has<Hittable>())
                {
                    ref var hittable = ref es[i].Get<Hittable>();
                    var boundingSphere = hittable.Collider.BoundingSphere(transform.ToColliderTransform());
                    SpriteBatch.DrawCircle(boundingSphere.Center.ToXnaVector2(), boundingSphere.Radius, 16, Color.Aqua);
                    
                    ref var box = ref hittable.SweptBox;
                    var pos = box.From.ToXnaVector2();
                    var size = (box.To - box.From).ToXnaVector2();
                    SpriteBatch.DrawRectangle(pos, size, Color.Yellow);
                }
            }
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
    }
}
