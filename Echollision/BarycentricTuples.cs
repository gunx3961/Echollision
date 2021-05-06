using System;

namespace ViLAWAVE.Echollision
{
    internal readonly ref struct BarycentricTuples<T> where T : struct
    {
        internal BarycentricTuples(Span<T> w, Span<float> lambda)
        {
            W = w;
            Lambda = lambda;
        }
        
        internal readonly Span<T> W;
        internal readonly Span<float> Lambda;
    }
}