using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results
{
    public abstract class ResultBase
    {
        protected TetrahedralMesh _ResultMesh;
        public CoreSolverResult CoreResult { get; }
        protected ResultBase(TetrahedralMesh mesh, CoreSolverResult coreResult)
        {
            _ResultMesh = mesh;
            CoreResult = coreResult;
        }
        protected IEnumerable<TetrahedronElement> GetElementsNodeBelongsTo(int nodeIdentifier)
        {
            if (_ResultMesh.MapNodeToElementsBelongsTo.TryGetValue(nodeIdentifier, out List<TetrahedronElement>? elements))
                if (elements.GroupBy(e => e.Identifier).Where(g => g.Count() > 1).Any()) { 
                
                }
                return elements;
            return Enumerable.Empty<TetrahedronElement>();
        }
        public void Print() {
            CoreResult.Print();
        }
    }
}