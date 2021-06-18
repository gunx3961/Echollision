using ViLAWAVE.Echollision;
using ViLAWAVE.Echollision.BroadPhase;

namespace MonoGameExample.Ecs
{
    public struct Hittable
    {
        public Collider Collider;
        public SweptBox SweptBox;
        public SweptCapsule SweptCapsule;
    }
}