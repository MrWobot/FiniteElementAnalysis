using Core.Collections;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public abstract class ResultBase3D
    {
        protected TetrahedralMesh _ResultMesh;
        public CoreSolverResult CoreResult { get; }
        protected ResultBase3D(TetrahedralMesh mesh, CoreSolverResult coreResult)
        {
            _ResultMesh = mesh;
            CoreResult = coreResult;
        }
        protected IEnumerable<TetrahedronElement> GetElementsNodeBelongsTo(int nodeIdentifier)
        {
            if (_ResultMesh.MapNodeToElementsBelongsTo.TryGetValue(nodeIdentifier, out List<TetrahedronElement>? elements))
                if (elements.GroupBy(e => e.Identifier).Where(g => g.Count() > 1).Any())
                {

                }
            return elements;
            return Enumerable.Empty<TetrahedronElement>();
        }
        public void Print()
        {
            CoreResult.Print();
        }
    }
}