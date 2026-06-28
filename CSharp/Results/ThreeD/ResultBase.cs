using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh.Interfaces;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.ThreeD
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
            if (_ResultMesh.MapNodeToElementsBelongsTo.TryGetValue(nodeIdentifier, out List<IElement>? elements))
                if (elements.GroupBy(e => e.Identifier).Where(g => g.Count() > 1).Any())
                {

                }
            return elements;
            return Enumerable.Empty<IElement>();
        }
        public void Print()
        {
            CoreResult.Print();
        }
    }
}