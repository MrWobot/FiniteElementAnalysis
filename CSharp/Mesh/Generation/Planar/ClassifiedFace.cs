using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Mesh.Generation.Planar
{

    // A face with a computed normal, classified after Step 2
    internal class ClassifiedFace
    {
        public RawFace RawFace { get; }
        public Vector3D Normal { get; }
        public bool IsParallelToNormalAxis { get; }  // true = volume/ignored face, false = boundary segment face
        public ClassifiedFace(RawFace rawFace, Vector3D normal, bool isParallelToNormalAxis)
        {
            RawFace = rawFace;
            Normal = normal;
            IsParallelToNormalAxis = isParallelToNormalAxis;
        }
    }
}
