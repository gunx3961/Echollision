using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public readonly ref struct MaxSupport
    {
        private readonly Span<Primitive> _primitives;

        public MaxSupport(in Span<Primitive> primitives)
        {
            _primitives = primitives;
        }

        internal Vector2 Center
        {
            get
            {
                var center = Vector2.Zero;
                for (var i = 0; i < _primitives.Length; i += 1)
                {
                    center += _primitives[i].Center;
                }

                return center / _primitives.Length;
            }
        }

        internal Vector2 Support(Vector2 normal)
        {
            var support = _primitives[0].Support(normal);
            var max = support;

            for (var i = 1; i < _primitives.Length; i += 1)
            {
                support = _primitives[i].Support(normal);
                if (Vector2.Dot(normal, support - max) > 0) max = support;
            }

            return max;
        }
    }
}