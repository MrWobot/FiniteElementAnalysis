using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results
{
    public abstract class ScalarResultBase : ResultBase
    {
        protected Dictionary<int, double> _MapNodeIdentifierToResultValue
            = new Dictionary<int, double>();
        protected ScalarResultBase(TetrahedralMesh mesh, CoreSolverResult basicResult)
            : base(mesh, basicResult)
        {
            foreach (Node node in mesh.Nodes)
            {
                _MapNodeIdentifierToResultValue[node.Identifier] = node.ScalarValue;
            }
        }
    }
}