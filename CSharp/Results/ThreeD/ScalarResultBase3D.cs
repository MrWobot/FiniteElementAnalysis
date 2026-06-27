using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public abstract class ScalarResultBase3D : ResultBase3D
    {
        protected Dictionary<int, double> _MapNodeIdentifierToResultValue
            = new Dictionary<int, double>();
        protected ScalarResultBase3D(TetrahedralMesh mesh, CoreSolverResult basicResult)
            : base(mesh, basicResult)
        {
            foreach (Node node in mesh.Nodes)
            {
                _MapNodeIdentifierToResultValue[node.Identifier] = node.ScalarValue;
            }
        }
    }
}