using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiniteElementAnalysis.Mesh.Generation
{
    internal class SkippedFaceFileEntry
    {
        public int[] Indices { get; }
        public Vector3D[] Points{ get; }
        public SkippedFaceFileEntry(int indexA, int indexB, int indexC,
            Vector3D pointA, Vector3D pointB, Vector3D pointC) {
            Indices = new int[] { indexA, indexB, indexC };
            Points = new Vector3D[] { pointA, pointB, pointC };
        }
    }
}
