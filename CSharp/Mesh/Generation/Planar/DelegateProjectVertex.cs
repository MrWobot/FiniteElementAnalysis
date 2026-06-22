using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Mesh.Generation.Planar
{

    // A face with a computed normal, classified after Step 2
    internal delegate (double a, double b) DelegateProjectVertex(Vector3D vertex);
}
