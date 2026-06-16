using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Mesh.Generation.ObjFileToPlanar_Internal
{

    // A face with a computed normal, classified after Step 2
    internal delegate double DelegateGetAxisCoordinate(Vector3D vertex);
}
