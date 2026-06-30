using Core;
using Core.Maths;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Results.Bases;

namespace FiniteElementAnalysis.Results
{
    public class LinearStaticAnalysisResult : VectorResultBase
    {
        public double[] Displacements => CoreResult.UnknownsVector;
        public LinearStaticAnalysisResult(IMesh mesh, CoreSolverResult coreResult)
            : base(mesh, coreResult)
        {

        }
        public IMesh DisplaceMesh()
        {
            return DisplaceMesh(_ResultMesh, Displacements);
        }
        public double[] CalculateMaxDisplacements()
        {

            double[] maxDisplacement = new double[CoreResult.NDegreesOfFreedom];
            int i = 0;
            double[] displacements = Displacements;
            while (i < displacements.Length)
            {
                for (int dof = 0; dof < CoreResult.NDegreesOfFreedom; dof++)
                {
                    double displacement = Math.Abs(displacements[i++]);
                    if (maxDisplacement[dof] < displacement)
                        maxDisplacement[dof] = displacement;
                }
            }
            return maxDisplacement;
        }
        public static IMesh DisplaceMesh(IMesh existingMesh, double[] displacements, bool toNewMesh = true)
        {
            IMesh newMesh = toNewMesh?existingMesh.Clone():existingMesh;
            int displacementsIndex = 0;
            if (newMesh.Nodes.Length < 1) return newMesh;
            if (displacements.Length != newMesh.Nodes.Length * newMesh.Nodes[0].Position.Length) {
                throw new DataMisalignedException();
            }
            foreach (INode newNode in newMesh.Nodes) {
                for (int positionIndex = 0; positionIndex < newNode.Position.Length; positionIndex++) {
                    newNode.Position[positionIndex] = displacements[displacementsIndex++];
                }
            }
            return newMesh;
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
                out double[]? ignore,
                out double[]? ignore2);

            int nDof = CoreResult.NDegreesOfFreedom;
            int nFieldComponents = CoreResult.NFieldComponents;
            int nNormal = nDof;
            int nShear = nFieldComponents - nDof;
            int nNodes = (nodalStressVector ?? nodalStrainVector)!.Length / nFieldComponents;

            nodalNormalStress = computeStress ? new double[nNodes * nNormal] : null;
            nodalShearStress = computeStress ? new double[nNodes * nShear] : null;
            nodalNormalStrain = computeStrain ? new double[nNodes * nNormal] : null;
            nodalShearStrain = computeStrain ? new double[nNodes * nShear] : null;

            void SplitVector(double[] source, double[]? normal, double[]? shear)
            {
                int index = 0;
                int normalIndex = 0;
                int shearIndex = 0;
                while (index < source.Length)
                {
                    for (int i = 0; i < nNormal; i++)
                        normal![normalIndex++] = source[index++];
                    for (int i = 0; i < nShear; i++)
                        shear![shearIndex++] = source[index++];
                }
            }

            if (computeStress)
                SplitVector(nodalStressVector!, nodalNormalStress, nodalShearStress);

            if (computeStrain)
                SplitVector(nodalStrainVector!, nodalNormalStrain, nodalShearStrain);
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
            nodalStressVector = createNodalStressVector ? new double[_ResultMesh.Nodes.Length * CoreResult.NFieldComponents] : null;
            nodalStrainVector = createNodalStrainVector ? new double[_ResultMesh.Nodes.Length * CoreResult.NFieldComponents] : null;
            elementsStressVector = createElementsStressVector ? new double[_ResultMesh.Elements.Length * CoreResult.NFieldComponents] : null;
            elementsStrainVector = createElementsStrainVector ? new double[_ResultMesh.Elements.Length * CoreResult.NFieldComponents] : null;
            int elementsIndex = 0;
            var mapNodeToStrainsVolumeSum = createNodalStrainVector ? new Dictionary<int, double[]>() : null;
            var mapNodeToStressVolumeSum = createNodalStressVector ? new Dictionary<int, double[]>() : null;
            var mapNodeToTotalElementsBelongsToVolume = createNodalStrainVector
                || createNodalStressVector ? new Dictionary<int, double>() : null;
            foreach (IElement element in _ResultMesh.Elements)
            {
                int nDof = CoreResult.NDegreesOfFreedom;
                double[] elementDisplacementVector = new double[element.Nodes.Length * nDof];
                int elementDisplacementIndex = 0;
                foreach (INode elementNode in element.Nodes)
                {
                    int globalDisplacementIndex = _ResultMesh.MapNodeIdentifierToGlobalIndex[elementNode.Identifier] * nDof;
                    for (int j = 0; j < nDof; j++)
                    {
                        elementDisplacementVector[elementDisplacementIndex++] = Displacements[globalDisplacementIndex++];
                    }
                }
                double[] strain = MatrixHelper.MatrixMultiplyByVector(
                    element.GetBMatrix(
                        CoreResult.NFieldComponents,
                        CoreResult.FieldOperationType, 
                        CoreResult.NDegreesOfFreedom),
                    elementDisplacementVector
                );
                if (createElementsStrainVector)
                {
                    Array.Copy(strain, 0, elementsStrainVector!, elementsIndex, CoreResult.NFieldComponents);
                }
                double[]? stress = null;
                if (createElementsStressVector || createNodalStressVector)
                {
                    Type volumeType = element.VolumeBelongsTo!.GetType();
                    if (!typeof(StaticLinearElasticVolume).IsAssignableFrom(volumeType))
                    {
                        throw new Exception($"The element with identifier {element.Identifier} does not belong to a volume assignable to type {typeof(StaticLinearElasticVolume)}. It has type {volumeType.Name}");
                    }
                    stress = MatrixHelper.MatrixMultiplyByVector(((StaticLinearElasticVolume)element.VolumeBelongsTo).ElasticityMatrix, strain);
                    if (createElementsStressVector)
                    {
                        Array.Copy(stress, 0, elementsStressVector!, elementsIndex, CoreResult.NFieldComponents);
                    }
                }
                if (mapNodeToTotalElementsBelongsToVolume != null)
                {
                    double elementVolume = element.Measure;
                    foreach (INode elementNode in element.Nodes)
                    {
                        if (mapNodeToTotalElementsBelongsToVolume.ContainsKey(elementNode.Identifier))
                        {
                            mapNodeToTotalElementsBelongsToVolume[elementNode.Identifier] += elementVolume;
                        }
                        else
                        {
                            mapNodeToTotalElementsBelongsToVolume[elementNode.Identifier] = elementVolume;
                        }

                        if (mapNodeToStrainsVolumeSum != null)
                        {
                            double[] strainTimesVolume = VectorHelper.Scale(strain, elementVolume);
                            if (mapNodeToStrainsVolumeSum!.TryGetValue(elementNode.Identifier, out double[]? nodalStrainVolumeSum))
                            {
                                VectorHelper.AddOntoFirstVector(nodalStrainVolumeSum, strainTimesVolume);
                            }
                            else
                            {
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
                elementsIndex += CoreResult.NFieldComponents;
            }
            if (createNodalStrainVector || createNodalStressVector)
            {
                int globalIndex = 0;
                foreach (INode node in _ResultMesh.Nodes)
                {
                    double totalElementsBelongsToVolume = mapNodeToTotalElementsBelongsToVolume![node.Identifier];
                    if (createNodalStrainVector)
                    {
                        double[] nodalStrains = VectorHelper.Scale(mapNodeToStrainsVolumeSum![node.Identifier], 1d / totalElementsBelongsToVolume);
                        Array.Copy(nodalStrains, 0, nodalStrainVector!, globalIndex, CoreResult.NFieldComponents);
                    }
                    if (createNodalStressVector)
                    {
                        double[] nodalStresses = VectorHelper.Scale(mapNodeToStressVolumeSum![node.Identifier], 1d / totalElementsBelongsToVolume);
                        Array.Copy(nodalStresses, 0, nodalStressVector!, globalIndex, CoreResult.NFieldComponents);
                    }
                    globalIndex += CoreResult.NFieldComponents;
                }
            }
        }
    }
}