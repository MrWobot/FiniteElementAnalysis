using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh.Interfaces;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.Bases
{
    public abstract class ResultBase
    {
        protected IMesh _ResultMesh;
        public CoreSolverResult CoreResult { get; }
        protected ResultBase(IMesh mesh, CoreSolverResult coreResult)
        {
            _ResultMesh = mesh;
            CoreResult = coreResult;
        }
        protected IEnumerable<IElement> GetElementsNodeBelongsTo(int nodeIdentifier)
        {
            return _ResultMesh.GetElementsThatNodeBelongsTo(nodeIdentifier);
        }
        public void Print()
        {
            CoreResult.Print();
        }
    }
}