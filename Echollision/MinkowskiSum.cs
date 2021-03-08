using System;
using System.Numerics;

namespace ViLAWAVE.Echollision
{
    public readonly ref struct MinkowskiSum
    {
        private readonly Span<Primitive> _primitives;

        public MinkowskiSum(in Span<Primitive> primitives)
        {
            _primitives = primitives;
        }
        
        internal Vector2 Center
        {
            get
            {
                var center = _primitives[0].Center;
                for (var i = 1; i < _primitives.Length; i += 1)
                {
                    center += _primitives[i].Center;
                }
                return center;
            }
        }
        
        internal Vector2 Support(Vector2 normal)
        {
            var support = _primitives[0].Support(normal);
            for (var i = 1; i < _primitives.Length; i += 1)
            {
                support += _primitives[i].Support(normal);
            }
            return support;
        }
    }
}