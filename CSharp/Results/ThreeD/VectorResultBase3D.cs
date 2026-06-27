using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public abstract class VectorResultBase3D : ResultBase3D
    {
        protected Dictionary<int, double[]> _MapNodeIdentifierToResultValue
            = new Dictionary<int, double[]>();
        protected VectorResultBase3D(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {
            int nodeIndex = 0;
            foreach (Node node in mesh.Nodes)
            {
                if (node.Values == null) throw new Exception($"{nameof(node.Values)} was null for node at index {nodeIndex}");
                _MapNodeIdentifierToResultValue[node.Identifier] = node.Values!;
                nodeIndex++;
            }
        }
    }
}