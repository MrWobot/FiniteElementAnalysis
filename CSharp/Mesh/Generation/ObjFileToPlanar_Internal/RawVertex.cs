using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Mesh.Generation.ObjFileToPlanar_Internal
{
    internal class RawVertex:Vector3D
    {
        public RawVertex(double x, double y, double z) :base(x, y, z) { }
    }
}
