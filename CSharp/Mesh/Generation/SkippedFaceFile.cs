using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiniteElementAnalysis.Mesh.Generation
{
    internal class SkippedFaceFile
    {
        public SkippedFaceFileEntryPair[] Pairs { get; }
        public SkippedFaceFile(SkippedFaceFileEntryPair[] pairs)
        {
            Pairs = pairs;
        }
        public override string ToString() { 
            StringBuilder sb = new StringBuilder();
            foreach(SkippedFaceFileEntryPair pair in Pairs)
            {
                sb.AppendLine(pair.ToString());
            }
            return sb.ToString();
        }
    }
}
