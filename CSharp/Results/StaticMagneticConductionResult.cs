using Core.Maths;
using Core.Maths.Tensors;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Interpolation;
using FiniteElementAnalysis.Mesh.Interfaces;

using FiniteElementAnalysis.Results.Bases;

namespace FiniteElementAnalysis.Results
{
    public class StaticMagneticConductionResult: VectorResultBase
    {
        public double[] NodalMagneticVectorPotentials => CoreResult.UnknownsVector;
        public StaticMagneticConductionResult(IMesh mesh, CoreSolverResult coreResult)
            : base(mesh, coreResult)
        {

        }
        public double[]? GetMagneticVectorPotentialAtPoint(double[] point)
        {
            return _ResultMesh.GetElementsContainingPoint(point)
                    .Where(e => e.IsPointInside(point))
                    .Select(e => e.InterpolateValueAtPoint(point, CoreResult.NDegreesOfFreedom))
            .FirstOrDefault();
        }
        private double[] GetElementMagneticFluxDensity(IElement element)
        {

            double[] nodalMagneticVectorPotentials =
                element.Nodes.SelectMany(n => _MapNodeIdentifierToResultValue[n.Identifier])
                .ToArray();
            return MatrixHelper.MatrixMultiplyByVector(
                    element.GetBMatrix(CoreResult.NFieldComponents, CoreResult.FieldOperationType, CoreResult.NDegreesOfFreedom),
                    nodalMagneticVectorPotentials
                );
        }
        public double CalculateFluxLinkage(double nTurns, int nPlanesToDivideBy, params MeasurementBoundary[] measurementBoundaries)
        {
            double magneticFlux = CalculateMagneticFlux(nPlanesToDivideBy, out double ignore, measurementBoundaries);
            return magneticFlux * nTurns;
        }
        public double CalculateFluxLinkage(double nTurns, int nPlanesToDivideBy, out double totalArea, params MeasurementBoundary[] measurementBoundaries)
        {
            double magneticFlux = CalculateMagneticFlux(nPlanesToDivideBy, out totalArea, measurementBoundaries);
            return magneticFlux * nTurns;
        }
        public double CalculateMagneticFlux(int nPlanesToDivideBy, params MeasurementBoundary[] measurementBoundaries)
        {
            return CalculateMagneticFlux(nPlanesToDivideBy, out double ignore, measurementBoundaries);
        }
        public double CalculateMagneticFlux(int nPlanesToDivideBy, out double totalArea, params MeasurementBoundary[] measurementBoundaries)
        {
            double totalFluxLinkage = 0;
            totalArea = 0;
            foreach (var measurementBoundary in measurementBoundaries)
            {
                IReadOnlyList<IBoundaryPrimitive> faces = _ResultMesh.GetPrimitivesForBoundary(measurementBoundary);
                if (faces == null) throw new Exception($"No faces for boundary named\"{measurementBoundary.Name}\"");
                foreach (IBoundaryPrimitive boundaryPrimitive in faces)
                {
                    var faceElementInterestedIn = boundaryPrimitive.Elements
                        .Where(e => 
                            VectorHelper.DotProduct(
                                VectorHelper.Subtract(e.Centroid, boundaryPrimitive.Centre), 
                                boundaryPrimitive.UnitNormal
                                ) >= 0);
                    if (faceElementInterestedIn.Count() > 1)
                    {
                        throw new Exception("Something went wrong");
                    }
                    IElement elementOneSideOfFace = faceElementInterestedIn.First();
                    double[] fluxDensity = GetElementMagneticFluxDensity(elementOneSideOfFace);
                    double fluxNormalToFace = VectorHelper.DotProduct(fluxDensity, boundaryPrimitive.UnitNormal);
                    double thicknessIntegral = boundaryPrimitive.Nodes
                        .Sum(n => _ResultMesh.GetThickness(n.Position)) / boundaryPrimitive.Nodes.Length;
                    double faceArea = boundaryPrimitive.Measure * thicknessIntegral;
                    double fluxLinkageForFace = Math.Abs(fluxNormalToFace * faceArea);
                    totalFluxLinkage += fluxLinkageForFace;
                    totalArea += faceArea;
                }
            }
            totalArea /= nPlanesToDivideBy;
            return totalFluxLinkage / nPlanesToDivideBy;
        }
        public double[] GetNodalMagneticFluxDensityB()
        {
            double[] values = new double[_ResultMesh.Nodes.Count * _ResultMesh.NodePositionLength];
            var mapElementIdentifierToFlux = new Dictionary<int, double[]>();
            foreach (var element in _ResultMesh.Elements)
            {
                mapElementIdentifierToFlux[element.Identifier] = GetElementMagneticFluxDensity(element);
            }
            foreach (INode node in _ResultMesh.Nodes)
            {
                var elementsNodeBelongsTo = _ResultMesh.GetElementsThatNodeBelongsTo(node.Identifier);
                double[] nodalFluxDensity = InterpolationHelper.InverseDistanceWeighting(node.Position,
                    elementsNodeBelongsTo
                    .Select(e => (e.Centroid, mapElementIdentifierToFlux[e.Identifier]))
                    .ToList(), power: 3);
                int valuesStartIndex = _ResultMesh.GetGlobalIndexForNode(node.Identifier);
                for (int i = 0; i < _ResultMesh.NodePositionLength; i++)
                {
                    values[(valuesStartIndex * _ResultMesh.NodePositionLength) + i] = nodalFluxDensity[i];
                }
            }
            return values;
        }
        /*
        public double CalculateWindingSelfInductance(
            double current,
            TetrahedralMesh forMesh,
            StaticCurrentConductionResult3D currentDensitiesForWindingMesh)
        {
            throw new NotImplementedException();/*
            double[] nodeVolumetricCurrentDensitiesForMesh =
                currentDensitiesForWindingMesh.GetNodalVolumetricCurrentDensities(forMesh);
            var forElements = forMesh.Elements;
            double wmagTotal = 0;
            foreach(TetrahedronElement forElement in forElements)
            {
                wmagTotal+=CalculateWindingElementMagneticEnergyWmag(forMesh, forElement, nodeVolumetricCurrentDensitiesForMesh);
            }
            return 2d*wmagTotal/Math.Pow(current, 2);
        }
        private double CalculateWindingElementMagneticEnergyWmag(
            TetrahedralMesh forMesh, TetrahedronElement element, double[] nodalVolumetricCurrentDensitiesForMesh)
        {
            var mapNodeToMagneticVectorPotential = _MapNodeIdentifierToResultValue;
            double[] nodalMagneticVectorPotentials = new double[12];
            double[] nodalVolumetricCurrentDensities = new double[12];
            int i = 0;
            foreach (var node in element.Nodes)
            {
                int nodeGlobalIndex = forMesh.MapNodeIndexToGlobalIndex[node.Index];
                double[] magneticVectorPotential = mapNodeToMagneticVectorPotential[node.Index];
                nodalMagneticVectorPotentials[i] = magneticVectorPotential[0];
                nodalVolumetricCurrentDensities[i++] = nodalVolumetricCurrentDensitiesForMesh[nodeGlobalIndex++];
                nodalMagneticVectorPotentials[i] = magneticVectorPotential[1];
                nodalVolumetricCurrentDensities[i++] = nodalVolumetricCurrentDensitiesForMesh[nodeGlobalIndex++];
                nodalMagneticVectorPotentials[i] = magneticVectorPotential[2];
                nodalVolumetricCurrentDensities[i++] = nodalVolumetricCurrentDensitiesForMesh[nodeGlobalIndex];
            }
            return 0.5d
                    * VectorHelper.DotProduct(nodalMagneticVectorPotentials,
                        nodalVolumetricCurrentDensities)
                    * element.ElementVolume;
        }*/
    }
}