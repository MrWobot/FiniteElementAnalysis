using Core;
using Core.Maths;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public class LinearStaticAnalysisResult3D : VectorResultBase
    {
        public double[] Displacements => CoreResult.UnknownsVector;
        public LinearStaticAnalysisResult3D(IMesh mesh, CoreSolverResult coreResult)
            : base(mesh, coreResult)
        {

        }
        public IMesh DisplaceMesh()
        {
            return DisplaceMesh(_ResultMesh, Displacements);
        }
        public (double maxX, double maxY, double maxZ) CalculateMaxDisplacements()
        {

            double maxDisplacementX = 0;
            double maxDisplacementY = 0;
            double maxDisplacementZ = 0;
            int i = 0;
            double[] displacements = Displacements;
            while (i < displacements.Length)
            {
                double displacementX = Math.Abs(displacements[i++]);
                if (maxDisplacementX < displacementX)
                    maxDisplacementX = displacementX;
                double displacementY = Math.Abs(displacements[i++]);
                if (maxDisplacementY < displacementY)
                    maxDisplacementY = displacementY;
                double displacementZ = Math.Abs(displacements[i++]);
                if (maxDisplacementZ < displacementZ)
                    maxDisplacementZ = displacementZ;
            }
            return (maxDisplacementX, maxDisplacementY, maxDisplacementZ);
        }
        public static IMesh DisplaceMesh(IMesh existingMesh, double[] displacements)
        {
            throw new NotImplementedException("This was not implemented properly its not parsing new nodes to new mesh and stuff and needs to be implemented so it works for all types of mesh now using IMesh");
            /*
            Node[] newNodes = new Node[existingMesh.Nodes.Length];
            int displacementsIndex = 0;
            int nodeIndex = 0;
            foreach (Node existingNode in existingMesh.Nodes)
            {
                Node newNode = new Node(
                    existingNode.Index,
                    existingNode.X + displacements[displacementsIndex++],
                    existingNode.Y + displacements[displacementsIndex++],
                    existingNode.Z + displacements[displacementsIndex++],
                    existingNode.Attributes,
                    existingNode.Boundary);
                newNodes[nodeIndex++] = newNode;
            }
            return new TetrahedralMesh(existingMesh.Boundaries, existingMesh.Volumes, newNodes, existingMesh.BoundaryFaces, existingMesh.Elements, existingMesh.ElementsBVHTree);
            */
        }
        public void ComputeNodalNormalAndShearStressStrainAsSeperateVectors(
            bool computeStress, bool computeStrain,
            out double[]? nodalNormalStress, out double[]? nodalShearStress,
            out double[]? nodalNormalStrain, out double[]? nodalShearStrain)
        {

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
                    nodalShearStress![specificIndex++] = nodalStressVector[index++];
                    nodalShearStress[specificIndex++] = nodalStressVector[index++];
                    nodalShearStress[specificIndex++] = nodalStressVector[index++];
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
                    nodalShearStrain![specificIndex++] = nodalStrainVector[index++];
                    nodalShearStrain[specificIndex++] = nodalStrainVector[index++];
                    nodalShearStrain[specificIndex++] = nodalStrainVector[index++];
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
            foreach (IElement element in _ResultMesh.Elements)
            {
                double[] elementDisplacementVector = new double[12];
                int elementDisplacementIndex = 0;
                foreach (INode elementNode in element.Nodes)
                {
                    int globalDisplacementIndex = _ResultMesh.MapNodeIndexToGlobalIndex[elementNode.Index]
                        * 3;
                    for (int j = 0; j < 3; j++)
                    {
                        elementDisplacementVector[elementDisplacementIndex++] = Displacements[globalDisplacementIndex++];
                    }
                }
                double[] strain = MatrixHelper.MatrixMultiplyByVector(element.BMatrix3DOF6FieldComponentsStrainDisplacement, elementDisplacementVector);
                if (createElementsStrainVector)
                {
                    Array.Copy(strain, 0, elementsStrainVector!, elementsIndex, 6);
                }
                double[]? stress = null;
                if (createElementsStressVector || createNodalStressVector)
                {
                    Type volumeType = element.VolumeBelongsTo!.GetType();
                    if (!typeof(StaticLinearElasticVolume).IsAssignableFrom(volumeType))
                    {
                        throw new Exception($"The element with identifier {element.Index} does not belong to a volume assignable to type {typeof(StaticLinearElasticVolume)}. It has type {volumeType.Name}");
                    }
                    stress = MatrixHelper.MatrixMultiplyByVector(((StaticLinearElasticVolume)element.VolumeBelongsTo).ElasticityMatrix, strain);
                    if (createElementsStressVector)
                    {
                        Array.Copy(stress, 0, elementsStressVector!, elementsIndex, 6);
                    }
                }
                if (mapNodeToTotalElementsBelongsToVolume != null)
                {
                    double elementVolume = element.Measure;
                    foreach (INode elementNode in element.Nodes)
                    {
                        if (mapNodeToTotalElementsBelongsToVolume.ContainsKey(elementNode.Index))
                        {
                            mapNodeToTotalElementsBelongsToVolume[elementNode.Index] += elementVolume;
                        }
                        else
                        {
                            mapNodeToTotalElementsBelongsToVolume[elementNode.Index] = elementVolume;
                        }

                        if (mapNodeToStrainsVolumeSum != null)
                        {
                            double[] strainTimesVolume = VectorHelper.Scale(strain, elementVolume);
                            if (mapNodeToStrainsVolumeSum!.TryGetValue(elementNode.Index, out double[]? nodalStrainVolumeSum))
                            {
                                VectorHelper.AddOntoFirstVector(nodalStrainVolumeSum, strainTimesVolume);
                            }
                            else
                            {
                                mapNodeToStrainsVolumeSum[elementNode.Index] = strainTimesVolume;
                            }

                        }
                        if (mapNodeToStressVolumeSum != null)
                        {
                            double[] stressTimesVolume = VectorHelper.Scale(stress!, elementVolume);
                            if (mapNodeToStressVolumeSum!.TryGetValue(elementNode.Index, out double[]? nodalStressVolumeSum))
                            {
                                VectorHelper.AddOntoFirstVector(nodalStressVolumeSum, stressTimesVolume);
                            }
                            else
                            {
                                mapNodeToStressVolumeSum[elementNode.Index] = stressTimesVolume;
                            }

                        }
                    }
                }
                elementsIndex += 6;
            }
            if (createNodalStrainVector || createNodalStressVector)
            {
                int globalIndex = 0;
                foreach (INode node in _ResultMesh.Nodes)
                {
                    double totalElementsBelongsToVolume = mapNodeToTotalElementsBelongsToVolume![node.Index];
                    if (createNodalStrainVector)
                    {
                        double[] nodalStrains = VectorHelper.Scale(mapNodeToStrainsVolumeSum![node.Index], 1d / totalElementsBelongsToVolume);
                        Array.Copy(nodalStrains, 0, nodalStrainVector!, globalIndex, 6);
                    }
                    if (createNodalStressVector)
                    {
                        double[] nodalStresses = VectorHelper.Scale(mapNodeToStressVolumeSum![node.Index], 1d / totalElementsBelongsToVolume);
                        Array.Copy(nodalStresses, 0, nodalStressVector!, globalIndex, 6);
                    }
                    globalIndex += 6;
                }
            }
        }
    }
}