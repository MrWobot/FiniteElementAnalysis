using Core.Maths;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Results.Bases;

namespace FiniteElementAnalysis.Results
{
    public class ElectrostaticsResult : ScalarResultBase
    {
        public double[] Potentials => CoreResult.UnknownsVector;
        public ElectrostaticsResult(IMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
        public double[] GetNodalScalarElectricFieldIntensities()
        {
            var mapElementToElectricFieldStrength = new Dictionary<IElement, double[]>();
            foreach (var element in _ResultMesh.Elements)
            {
                double[][] elementBMatrix = element.GetBMatrix(1, FieldOperationType.Gradient, 1);
                double[] E = VectorHelper.Scale(MatrixHelper.MatrixMultiplyByVector(elementBMatrix, element.Nodes.Select(n => Potentials[_ResultMesh.GetGlobalIndexForNode(n.Identifier)]).ToArray()), -1);
                mapElementToElectricFieldStrength[element] = E;

            }
            double[] nodeEs = new double[_ResultMesh.Nodes.Count];
            foreach (var node in _ResultMesh.Nodes)
            {
                IReadOnlySet<IElement> elements = _ResultMesh.GetElementsThatNodeBelongsTo(node.Identifier);
                double[] sum = new double[_ResultMesh.NodePositionLength];
                foreach (var element in elements)
                {
                    double[] elementE = mapElementToElectricFieldStrength[element];
                    for (int i = 0; i < _ResultMesh.NodePositionLength; i++)
                    {
                        sum[i] += elementE[i];
                    }
                }
                double[] nodeE = sum.Select(s => s / elements.Count).ToArray();
                double scalarNodeE = Math.Sqrt(nodeE.Sum(e => e * e));
                nodeEs[_ResultMesh.GetGlobalIndexForNode(node.Identifier)] = scalarNodeE;

            }
            return nodeEs;
        }
    }
}