using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results
{
    public abstract class VectorResultBase : ResultBase
    {
        protected Dictionary<int, double[]> _MapNodeIdentifierToResultValue
            = new Dictionary<int, double[]>();
        protected VectorResultBase(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
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