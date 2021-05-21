using System;
using DefaultEcs;
using Microsoft.Xna.Framework;
using MonoGameExample.Ecs;
using ViLAWAVE.Echollision.Collider;

namespace MonoGameExample
{
    public class Playground : Screen
    {
        public Playground(Framework framework) : base(framework)
        {
        }

        private EntitySet _obstructionSet;
        private EntitySet _bulletSet;

        public override void Initialize()
        {
            base.Initialize();

            _obstructionSet = World.GetEntities().With<Obstruction>().With<Transform2D>().AsSet();
            _bulletSet = World.GetEntities().With<Bullet>().With<Transform2D>().AsSet();
            
            // Obstructions
            var ob = World.CreateEntity();
            var obPosition = Framework.LogicalSize.ToVector2().ToSystemVector2() / 2f;
            ob.Set(new Transform2D {Position = obPosition, Rotation = 0f});
            ob.Set(new Obstruction { Speed = 100f, AngularSpeed = 0.5f * MathF.PI});
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        private SphereCollider _bulletCollider = new SphereCollider(0f);
        private ConvexCollider _obstructionCollider = new ConvexCollider(new[]
        {
            new System.Numerics.Vector2(-160, -160),
            new System.Numerics.Vector2(160, -160),
            new System.Numerics.Vector2(160, 160),
            new System.Numerics.Vector2(-160, 160)
        });

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
                Speed = 1000f,
                BirthTime = gameTime.TotalGameTime,
                LifeTime = 2f
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
                transform.Rotation += (float)(gameTime.ElapsedGameTime.TotalSeconds * obstruction.AngularSpeed);
            }
        }

        private void BulletSystem(GameTime gameTime)
        {
            
        }

        private struct Obstruction
        {
            public float Speed;
            public float AngularSpeed;
        }
    }
}