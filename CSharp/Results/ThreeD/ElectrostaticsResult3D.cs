using Core.Maths;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public class ElectrostaticsResult3D : ScalarResultBase
    {
        public double[] Potentials => CoreResult.UnknownsVector;
        public ElectrostaticsResult3D(IMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
        public double[] GetNodalScalarElectricFieldIntensities()
        {
            var mapElementToElectricFieldStrength = new Dictionary<IElement, double[]>();
            foreach (var element in _ResultMesh.Elements)
            {
                double[][] elementBMatrix = element.GetBMatrix(1, FieldOperationType.Gradient, 1);
                double[] E = VectorHelper.Scale(MatrixHelper.MatrixMultiplyByVector(elementBMatrix, element.Nodes.Select(n => Potentials[_ResultMesh.MapNodeIndexToGlobalIndex[n.Index]]).ToArray()), -1);
                mapElementToElectricFieldStrength[element] = E;

            }
            double[] nodeEs = new double[_ResultMesh.Nodes.Length];
            foreach (var node in _ResultMesh.Nodes)
            {
                List<IElement> elements = _ResultMesh.MapNodeToElementsBelongsTo[node.Index];
                double[] sum = new double[3];
                foreach (var element in elements)
                {
                    double[] elementE = mapElementToElectricFieldStrength[element];
                    sum[0] += elementE[0];
                    sum[1] += elementE[1];
                    sum[2] += elementE[2];
                }
                double[] nodeE = new double[] { sum[0] / elements.Count, sum[1] / elements.Count, sum[2] / elements.Count };
                double scalarNodeE = Math.Sqrt(Math.Pow(nodeE[0], 2) + Math.Pow(nodeE[1], 2) + Math.Pow(nodeE[2], 2));
                nodeEs[_ResultMesh.MapNodeIndexToGlobalIndex[node.Index]] = scalarNodeE;

            }
            return nodeEs;
        }
    }
}