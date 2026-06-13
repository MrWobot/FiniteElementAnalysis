using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiniteElementAnalysis.Mesh.Generation
{
    internal class SkippedFaceFileEntryPair
    {
        public SkippedFaceFileEntry EntryA { get; }
        public SkippedFaceFileEntry EntryB { get; }
        public SkippedFaceFileEntryPair(SkippedFaceFileEntry entryA, SkippedFaceFileEntry entryB) {
            EntryA = entryA;
            EntryB = entryB;
        }
        public override string ToString()
        {
            var matchingPoints = EntryA.Points.Where(p => EntryB.Points.Contains(p));
            return "Points: " + string.Join(", ",matchingPoints.Select(p => p.ToString()));
        }
    }
}
