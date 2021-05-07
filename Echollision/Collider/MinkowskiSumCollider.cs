﻿using System.Numerics;

namespace ViLAWAVE.Echollision.Collider
{
    public class MinkowskiSumCollider : ICollider
    {
        public MinkowskiSumCollider(ICollider[] shapes)
        {
            _shapes = shapes;
        }

        public Vector2 Center
        {
            get
            {
                var center = _shapes[0].Center;
                for (var i = 1; i < _shapes.Length; i += 1)
                {
                    center += _shapes[i].Center;
                }
                return center;
            }
        }

        public Vector2 Support(Vector2 direction)
        {
            var support = _shapes[0].Support(direction);
            for (var i = 1; i < _shapes.Length; i += 1)
            {
                support += _shapes[i].Support(direction);
            }
            return support;
        }

        private readonly ICollider[] _shapes;
    }
}