using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.BroadPhase;

namespace MonoGameExample.Ecs
{
    public struct Hittable
    {
        public Collider Collider;
        public Aabb SweptBox;
        public SweptCapsule SweptCapsule;
    }
}