using FiniteElementAnalysis.Mesh.Interfaces;;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public abstract class VectorResultBase : ResultBase
    {
        protected Dictionary<int, double[]> _MapNodeIdentifierToResultValue
            = new Dictionary<int, double[]>();
        protected VectorResultBase(IMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {
            foreach (INode node in mesh.Nodes)
            {
                if (node.Values == null) throw new Exception($"{nameof(node.Values)} was null for node at index {node.Index}");
                _MapNodeIdentifierToResultValue[node.Index] = node.Values!;
            }
        }
    }
}