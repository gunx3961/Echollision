using System.Collections.Generic;

namespace ViLAWAVE.Echollision.BroadPhase
{
    /// <summary>
    /// A 2D space for Sweep and Prune.
    /// </summary>
    public class SapSpace
    {
        public SapSpace()
        {
            // TODO: determine initial capacity
            // TODO: capacity extended by a certain step
            const int initialCaseCapacity = 32;
            _intersectionCases = new Dictionary<int, List<int>>(initialCaseCapacity);

            _boxBuffer = new List<SweptBox[]>();
            var firstChunk = new SweptBox[boxChunkSize];
            _boxBuffer.Add(firstChunk);
        }
        
        private readonly Dictionary<int, List<int>> _intersectionCases;
        
        private const int boxChunkSize = 512;
        private readonly List<SweptBox[]> _boxBuffer;

        private ref SweptBox GetBox(int index)
        {
            var chunk = _boxBuffer[index / boxChunkSize];
            return ref chunk[index % boxChunkSize];
        }
    }
}
