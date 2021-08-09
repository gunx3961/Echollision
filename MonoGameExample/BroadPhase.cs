using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using MonoGameExample.Ecs;
using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.BroadPhase;
using SystemVector2 = System.Numerics.Vector2;

namespace MonoGameExample
{
    public class BroadPhase : Screen
    {
        public BroadPhase(Framework framework) : base(framework)
        {
        }

        private List<Collider> _colliders;
        private Collision _collision;
        private Random _rs;

        private ISystem<GameTime> _logicalSystem;
        private ISystem<GameTime> _renderSystem;

        private EntitySet _lifeSet;
        private EntitySet _rigidBodySet;
        private EntitySet _hittableSet;

        private List<(int a, int b)> _boxIntersectionBuffer;
        private List<(int a, int b)> _capsuleIntersectionBuffer;

        public override void Initialize()
        {
            base.Initialize();

            _collision = new Collision();
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
            var yBox = ColliderFactory.Rect(y, y);
            yBox.InitializeBounding();

            var left = World.CreateEntity();
            left.Set(new Transform(new SystemVector2(-y, 0)));
            left.Set(new Hittable {Collider = yBox});

            var right = World.CreateEntity();
            right.Set(new Transform(new SystemVector2(x, 0)));
            right.Set(new Hittable {Collider = yBox});

            var ground = World.CreateEntity();
            ground.Set(new Transform(new SystemVector2(0, y)));
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

            _renderSystem = new SequentialSystem<GameTime>(
                new ActionSystem<GameTime>(DrawSystem),
                new ActionSystem<GameTime>(UIDrawSystem)
            );

            _boxIntersectionBuffer = new List<(int a, int b)>(512);
            _capsuleIntersectionBuffer = new List<(int a, int b)>(512);
        }

        private float _lastSpawnTime;
        private const float SpawnInterval = 0.03f;

        private void SpawnSystem(GameTime gameTime)
        {
            var currentTime = (float) gameTime.TotalGameTime.TotalSeconds;
            if (Framework.MouseState.IsButtonUp(MouseButton.Left) ||
                currentTime < _lastSpawnTime + SpawnInterval) return;

            _lastSpawnTime = currentTime;
            var randomCollider = _colliders[_rs.Next(0, _colliders.Count)];
            var e = World.CreateEntity();
            e.Set(new Life {BirthTime = (float) gameTime.TotalGameTime.TotalSeconds, Length = 5f});
            e.Set(new Hittable {Collider = randomCollider});

            _rs.NextUnitVector(out var randomDirection);
            var randomSpeed = _rs.NextSingle(2000, 20000);
            var vel = randomDirection.ToSystemVector2() * randomSpeed;
            // var vel = new SystemVector2(-1, 0.5f) * randomSpeed;
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
                transform.DestinationPosition = transform.Position +
                                                (float) gameTime.ElapsedGameTime.TotalSeconds * rigidBody.Velocity;
                transform.DestinationRotation = transform.Rotation +
                                                (float) gameTime.ElapsedGameTime.TotalSeconds *
                                                rigidBody.AngularVelocity;
            }
        }

        private void CollisionResolveSystem(GameTime gameTime)
        {
            var es = _hittableSet.GetEntities();

            // Span<SweptBox> boxes = stackalloc SweptBox[es.Length];
            for (var i = 0; i < es.Length; i += 1)
            {
                ref var transform = ref es[i].Get<Transform>();
                ref var hittable = ref es[i].Get<Hittable>();
                var colliderTransform = transform.ToColliderTransform();
                var movement = transform.DestinationPosition - transform.Position;
                var box = hittable.Collider.SweepBox(colliderTransform, movement);
                hittable.SweptBox = box;
                // boxes[i] = box;
            }

            Span<Entity> hittableEntities = stackalloc Entity[es.Length];
            es.CopyTo(hittableEntities);

            int CompareX(Entity a, Entity b) =>
                Math.Sign(a.Get<Hittable>().SweptBox.From.X - b.Get<Hittable>().SweptBox.From.X);

            int CompareY(SweptBox a, SweptBox b) => Math.Sign(a.From.Y - b.From.Y);

            hittableEntities.Sort(CompareX); // O(n Log(n))

            _boxIntersectionBuffer.Clear();
            _capsuleIntersectionBuffer.Clear();
            for (var i = 0; i < hittableEntities.Length; i += 1)
            {
                ref var box = ref hittableEntities[i].Get<Hittable>().SweptBox;

                var j = i + 1;
                while (j < hittableEntities.Length)
                {
                    ref var next = ref hittableEntities[j].Get<Hittable>().SweptBox;
                    if (next.From.X > box.To.X) break;
                    j += 1;
                    if (box.From.Y > next.To.Y || box.To.Y < next.From.Y) continue;
                    _boxIntersectionBuffer.Add((i, j - 1));
                }
            }

            for (var i = 0; i < _boxIntersectionBuffer.Count; i += 1)
            {
                var (aIndex, bIndex) = _boxIntersectionBuffer[i];
                ref var entityA = ref hittableEntities[aIndex];
                ref var transformA = ref entityA.Get<Transform>();
                ref var hittableA = ref entityA.Get<Hittable>();
                var colliderTransformA = transformA.ToColliderTransform();
                var movementA = transformA.Movement;
                var capsuleA = hittableA.Collider.SweptCapsule(colliderTransformA, movementA);

                ref var entityB = ref hittableEntities[bIndex];
                ref var transformB = ref entityB.Get<Transform>();
                ref var hittableB = ref entityB.Get<Hittable>();
                var colliderTransformB = transformB.ToColliderTransform();
                var movementB = transformB.Movement;
                var capsuleB = hittableB.Collider.SweptCapsule(colliderTransformB, movementB);

                if (!SweptCapsule.Intersection(ref capsuleA, ref capsuleB))
                {
                    // TODO: intersection debug
                    System.Diagnostics.Debugger.Break();
                    continue;
                }
                _capsuleIntersectionBuffer.Add((aIndex, bIndex));

                var realCollision = _collision.Continuous(
                    hittableA.Collider, colliderTransformA, movementA,
                    hittableB.Collider, colliderTransformB, movementB,
                    out var toi, out var n
                );

                if (!realCollision) continue;

                var aHasRigidBody = entityA.Has<RigidBody>();
                var bHasRigidBody = entityB.Has<RigidBody>();
                if (!(aHasRigidBody || bHasRigidBody)) continue;

                var t = (float) gameTime.ElapsedGameTime.TotalSeconds;
                // Resolve penetration
                if (toi == 0f)
                {
                    _collision.Penetration(
                        hittableA.Collider, colliderTransformA,
                        hittableB.Collider, colliderTransformB,
                        out n, out var depth
                    );
                    n = SystemVector2.Normalize(n);
                    
                    if (aHasRigidBody)
                    {
                        ref var rigidBody = ref entityA.Get<RigidBody>();
                        var newVel = rigidBody.Velocity.Length() * n;
                        transformA.DestinationPosition = transformA.Position + t * newVel;
                        rigidBody.Velocity = newVel;
                    }

                    if (bHasRigidBody)
                    {
                        ref var rigidBody = ref entityB.Get<RigidBody>();
                        var newVel = rigidBody.Velocity.Length() * -n;
                        transformB.DestinationPosition = transformB.Position + t * newVel;
                        rigidBody.Velocity = newVel;
                    }
                }
                // Resolve priori collision
                else
                {
                    n = SystemVector2.Normalize(n);
                    if (aHasRigidBody)
                    {
                        ref var rigidBody = ref entityA.Get<RigidBody>();
                        var hitPoint = transformA.Position + rigidBody.Velocity * toi * t;
                        SystemVector2 newVel;
                        if (SystemVector2.Dot(rigidBody.Velocity, n) <= 0)
                        {
                            newVel = SystemVector2.Reflect(rigidBody.Velocity, n);
                        }
                        else
                        {
                            newVel = SystemVector2.Normalize(SystemVector2.Normalize(rigidBody.Velocity) + n) * rigidBody.Velocity.Length();
                        }
                        transformA.DestinationPosition = hitPoint + newVel * (1f - toi) * t;
                        rigidBody.Velocity = newVel;
                    }

                    if (bHasRigidBody)
                    {
                        ref var rigidBody = ref entityB.Get<RigidBody>();
                        var hitPoint = transformB.Position + rigidBody.Velocity * toi * t;
                        SystemVector2 newVel;
                        if (SystemVector2.Dot(rigidBody.Velocity, n) >= 0)
                        {
                            newVel = SystemVector2.Reflect(rigidBody.Velocity, -n);
                        }
                        else
                        {
                            newVel = SystemVector2.Normalize(SystemVector2.Normalize(rigidBody.Velocity) - n) * rigidBody.Velocity.Length();
                        }
                        transformB.DestinationPosition = hitPoint + newVel * (1f - toi) * t;
                        rigidBody.Velocity = newVel;
                    }
                }
            }

            _info.ObjectCount = es.Length;
            _info.BoxIntersectionCount = _boxIntersectionBuffer.Count;
            _info.CapsuleIntersectionCount = _capsuleIntersectionBuffer.Count;
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

        private struct Info
        {
            public int ObjectCount;
            public int BoxIntersectionCount;
            public int CapsuleIntersectionCount;
        }

        private Info _info;

        private void UIDrawSystem(GameTime gameTime)
        {
            var font = Framework.Resource.GetFont(GlobalResource.Font.DefaultPixel);
            var pos = new Vector2(16);
            var lineHeight = new Vector2(0, 16);
            SpriteBatch.DrawString(font,
                $"Object: {_info.ObjectCount.ToString()}, n^2: {(_info.ObjectCount * _info.ObjectCount).ToString()}",
                pos, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            pos += lineHeight;
            SpriteBatch.DrawString(font, $"Box Intersection: {_info.BoxIntersectionCount.ToString()}", pos, Color.White,
                0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            pos += lineHeight;
            SpriteBatch.DrawString(font, $"Capsule Intersection: {_info.CapsuleIntersectionCount.ToString()}", pos,
                Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
        }

        private void DrawSystem(GameTime gameTime)
        {
            var es = _rigidBodySet.GetEntities();
            for (var i = 0; i < es.Length; i += 1)
            {
                ref var transform = ref es[i].Get<Transform>();
                SpriteBatch.DrawCircle(transform.Position.ToXnaVector2(), 32f, 16, Color.White);

                // if (es[i].Has<Hittable>())
                // {
                //     ref var hittable = ref es[i].Get<Hittable>();
                //     var boundingSphere = hittable.Collider.BoundingSphere(transform.ToColliderTransform());
                //     SpriteBatch.DrawCircle(boundingSphere.Center.ToXnaVector2(), boundingSphere.Radius, 16, Color.Aqua);
                //
                //     ref var box = ref hittable.SweptBox;
                //     var pos = box.From.ToXnaVector2();
                //     var size = (box.To - box.From).ToXnaVector2();
                //     SpriteBatch.DrawRectangle(pos, size, Color.Yellow);
                // }
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
