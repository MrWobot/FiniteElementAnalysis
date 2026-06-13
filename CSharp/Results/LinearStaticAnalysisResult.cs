using Core.Maths;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;

namespace FiniteElementAnalysis.Results
{
    public class LinearStaticAnalysisResult : VectorResultBase
    {
        public double[] Displacements => CoreResult.UnknownsVector; 
        public LinearStaticAnalysisResult(TetrahedralMesh mesh, CoreSolverResult coreResult)
            : base(mesh, coreResult)
        {

        }
        public TetrahedralMesh DisplaceMesh()
        {
            return DisplaceMesh(_ResultMesh, Displacements);
        }
        public static TetrahedralMesh DisplaceMesh(TetrahedralMesh existingMesh, double[] displacements) {
            Node[] newNodes = new Node[existingMesh.Nodes.Length];
            int displacementsIndex = 0;
            int nodeIndex = 0;
            foreach( Node existingNode in existingMesh.Nodes )
            {
                Node newNode = new Node(
                    existingNode.Identifier,
                    existingNode.X + displacements[displacementsIndex++],
                    existingNode.Y + displacements[displacementsIndex++],
                    existingNode.Z + displacements[displacementsIndex++],
                    existingNode.Attributes,
                    existingNode.Boundary);
                newNodes[nodeIndex++]= newNode;
            }
            return new TetrahedralMesh(existingMesh.Boundaries, existingMesh.Volumes, newNodes, existingMesh.BoundaryFaces, existingMesh.Elements, existingMesh.ElementsBVHTree);
        }
        public void ComputeNodalNormalAndShearStressStrainAsSeperateVectors(
            bool computeStress, bool computeStrain,
            out double[]? nodalNormalStress, out double[]? nodalShearStress, 
            out double[]? nodalNormalStrain, out double[]? nodalShearStrain) {

            ComputeStressStrain(
                createNodalStressVector: computeStress,
                createNodalStrainVector: computeStrain,
                createElementsStressVector: false,
                createElementsStrainVector: false,
                out double[]? nodalStressVector,
                out double[]? nodalStrainVector,
                out double[] ignore,
                out double[] ignore2);
            nodalNormalStress = computeStress ? new double[nodalStressVector!.Length / 2] : null;
            nodalShearStress = computeStress ? new double[nodalStressVector!.Length / 2] : null;
            nodalNormalStrain = computeStrain ? new double[nodalStrainVector!.Length / 2] : null;
            nodalShearStrain = computeStrain ? new double[nodalStrainVector!.Length / 2] : null;
            int index = 0;
            int specificIndex = 0;
            if (computeStress)
            {
                while (index < nodalStressVector!.Length)
                {
                    nodalNormalStress![specificIndex] = nodalStressVector[index++];
                    nodalNormalStress[specificIndex + 1] = nodalStressVector[index++];
                    nodalNormalStress[specificIndex + 2] = nodalStressVector[index++];
                    nodalShearStress![specificIndex ++] = nodalStressVector[index++];
                    nodalShearStress[specificIndex ++] = nodalStressVector[index++];
                    nodalShearStress[specificIndex ++] = nodalStressVector[index++];
                }
                specificIndex = 0;
                index = 0;
            }
            if (computeStrain)
            {
                while (index < nodalStrainVector!.Length)
                {
                    nodalNormalStrain![specificIndex] = nodalStrainVector[index++];
                    nodalNormalStrain[specificIndex + 1] = nodalStrainVector[index++];
                    nodalNormalStrain[specificIndex + 2] = nodalStrainVector[index++];
                    nodalShearStrain![specificIndex ++] = nodalStrainVector[index++];
                    nodalShearStrain[specificIndex ++] = nodalStrainVector[index++];
                    nodalShearStrain[specificIndex ++] = nodalStrainVector[index++];
                }
            }
        }
        public void ComputeStressStrain(
            bool createNodalStressVector,
            bool createNodalStrainVector,
            bool createElementsStressVector,
            bool createElementsStrainVector,
            out double[]? nodalStressVector,
            out double[]? nodalStrainVector,
            out double[]? elementsStressVector,
            out double[]? elementsStrainVector)
        {
            nodalStressVector = createNodalStressVector ? new double[_ResultMesh.Nodes.Length * 6] : null;
            nodalStrainVector = createNodalStrainVector ? new double[_ResultMesh.Nodes.Length * 6] : null;
            elementsStressVector = createElementsStressVector ? new double[_ResultMesh.Elements.Length * 6] : null;
            elementsStrainVector = createElementsStrainVector ? new double[_ResultMesh.Elements.Length * 6] : null;
            int elementsIndex = 0;
            var mapNodeToStrainsVolumeSum = createNodalStrainVector ? new Dictionary<int, double[]>() : null;
            var mapNodeToStressVolumeSum = createNodalStressVector ? new Dictionary<int, double[]>() : null;
            var mapNodeToTotalElementsBelongsToVolume = createNodalStrainVector 
                || createNodalStressVector ? new Dictionary<int, double>() : null;
            foreach (var element in _ResultMesh.Elements)
            {
                double[] elementDisplacementVector = new double[12];
                int elementDisplacementIndex = 0;
                foreach (Node elementNode in element.Nodes) {
                    int globalDisplacementIndex = _ResultMesh.MapNodeIdentifierToGlobalIndex[elementNode.Identifier]
                        *3;
                    for (int j = 0; j < 3; j++) {
                        elementDisplacementVector[elementDisplacementIndex++] = Displacements[globalDisplacementIndex++];
                    }
                }
                double[] strain= MatrixHelper.MatrixMultiplyByVector(element.BMatrix3DOF6FieldComponentsStrainDisplacement, elementDisplacementVector);
                if (createElementsStrainVector)
                {
                    Array.Copy(strain, 0, elementsStrainVector!, elementsIndex, 6);
                }
                double[]? stress = null;
                if (createElementsStressVector|| createNodalStressVector)
                {
                    Type volumeType = element.VolumeIsAPartOf!.GetType();
                    if (!typeof(StaticLinearElasticVolume).IsAssignableFrom(volumeType))
                    {
                        throw new Exception($"The element with identifier {element.Identifier} does not belong to a volume assignable to type {typeof(StaticLinearElasticVolume)}. It has type {volumeType.Name}");
                    }
                    stress = MatrixHelper.MatrixMultiplyByVector(((StaticLinearElasticVolume)element.VolumeIsAPartOf).ElasticityMatrix, strain);
                    if (createElementsStressVector)
                    {
                        Array.Copy(stress, 0, elementsStressVector!, elementsIndex, 6);
                    }
                }
                if (mapNodeToTotalElementsBelongsToVolume != null)
                {
                    double elementVolume = element.ElementVolume;
                    foreach (Node elementNode in element.Nodes)
                    {
                        if (mapNodeToTotalElementsBelongsToVolume.ContainsKey(elementNode.Identifier))
                        {
                            mapNodeToTotalElementsBelongsToVolume[elementNode.Identifier] += elementVolume;
                        }
                        else
                        {
                            mapNodeToTotalElementsBelongsToVolume[elementNode.Identifier] = elementVolume;
                        }

                        if (mapNodeToStrainsVolumeSum!=null)
                        {
                            double[] strainTimesVolume = VectorHelper.Scale(strain, elementVolume);
                            if (mapNodeToStrainsVolumeSum!.TryGetValue(elementNode.Identifier, out double[]? nodalStrainVolumeSum))
                            {
                                VectorHelper.AddOntoFirstVector(nodalStrainVolumeSum, strainTimesVolume);
                            }
                            else {
                                mapNodeToStrainsVolumeSum[elementNode.Identifier] = strainTimesVolume;
                            }

                        }
                        if (mapNodeToStressVolumeSum != null)
                        {
                            double[] stressTimesVolume = VectorHelper.Scale(stress!, elementVolume);
                            if (mapNodeToStressVolumeSum!.TryGetValue(elementNode.Identifier, out double[]? nodalStressVolumeSum))
                            {
                                VectorHelper.AddOntoFirstVector(nodalStressVolumeSum, stressTimesVolume);
                            }
                            else
                            {
                                mapNodeToStressVolumeSum[elementNode.Identifier] = stressTimesVolume;
                            }

                        }
                    }
                }
                elementsIndex += 6;
            }
            if (createNodalStrainVector || createNodalStressVector) {
                int globalIndex = 0;
                foreach (var node in _ResultMesh.Nodes) {
                    double totalElementsBelongsToVolume = mapNodeToTotalElementsBelongsToVolume![node.Identifier];
                    if (createNodalStrainVector) {
                        double[] nodalStrains = VectorHelper.Scale(mapNodeToStrainsVolumeSum![node.Identifier], 1d / totalElementsBelongsToVolume);
                        Array.Copy(nodalStrains, 0, nodalStrainVector!, globalIndex, 6);
                    }
                    if (createNodalStressVector)
                    {
                        double[] nodalStresses = VectorHelper.Scale(mapNodeToStressVolumeSum![node.Identifier], 1d / totalElementsBelongsToVolume);
                        Array.Copy(nodalStresses, 0, nodalStressVector!, globalIndex, 6);
                    }
                    globalIndex += 6;
                }
            }
        }
    }
}