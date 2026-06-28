using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh.Interfaces;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public abstract class ScalarResultBase : ResultBase
    {
        protected Dictionary<int, double> _MapNodeIndexToResultValue
            = new Dictionary<int, double>();
        protected ScalarResultBase(IMesh mesh, CoreSolverResult basicResult)
            : base(mesh, basicResult)
        {
            foreach (INode node in mesh.Nodes)
            {
                _MapNodeIndexToResultValue[node.Index] = node.ScalarValue;
            }
        }
    }
}