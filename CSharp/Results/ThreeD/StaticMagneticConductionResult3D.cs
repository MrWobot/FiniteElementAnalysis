using Core.Maths;
using Core.Maths.Tensors;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Interpolation;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public class StaticMagneticConductionResult3D : VectorResultBase
    {
        public double[] NodalMagneticVectorPotentials => CoreResult.UnknownsVector;
        public StaticMagneticConductionResult3D(TetrahedralMesh mesh, CoreSolverResult coreResult)
            : base(mesh, coreResult)
        {

        }
        public double[]? GetMagneticVectorPotentialAtPoint(Vector3D point)
        {
            return _ResultMesh.ElementsBVHTree.QueryBVH(point)
                    .Where(e => e.IsPointInside(point))
                    .Select(e => e.InterpolateValueAtPoint(point, 3))
            .FirstOrDefault();
        }
        private Vector3D GetElementMagneticFluxDensity(TetrahedronElement element)
        {

            double[] nodalMagneticVectorPotentials =
                element.Nodes.SelectMany(n => _MapNodeIdentifierToResultValue[n.Index])
                .ToArray();
            return Vector3D.FromArray(
                MatrixHelper.MatrixMultiplyByVector(
                    element.BMatrix3DOF3FieldComponentsUsingCurl,
                    nodalMagneticVectorPotentials
                )
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
                var faces = _ResultMesh.GetFacesForBoundary(measurementBoundary);
                if (faces == null) throw new Exception($"No faces for boundary named\"{measurementBoundary.Name}\"");
                foreach (BoundaryFace face in faces)
                {
                    var faceElementInterstedIn = face.Elements.Where(e => (e.GetCentroid() - face.CenterPoint).Dot(face.Normal) >= 0);
                    if (faceElementInterstedIn.Count() > 1)
                    {
                        throw new Exception("Something went wrong");
                    }
                    TetrahedronElement elementOneSideOfFace = faceElementInterstedIn.First();
                    Vector3D fluxDensity = GetElementMagneticFluxDensity(elementOneSideOfFace);
                    Vector3D unitDirectionFace = face.Normal;
                    double fluxNormalToFace = fluxDensity.Dot(unitDirectionFace);
                    double faceArea = face.Area;
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
            double[] values = new double[_ResultMesh.Nodes.Length * 3];
            Dictionary<int, Vector3D> mapElementToFlux = new Dictionary<int, Vector3D>();
            foreach (var element in _ResultMesh.Elements)
            {
                mapElementToFlux[element.Identifier] = GetElementMagneticFluxDensity(element);
            }
            foreach (Node node in _ResultMesh.Nodes)
            {
                var elementsNodeBelongsTo = _ResultMesh.MapNodeToElementsBelongsTo[node.Index];
                Vector3D nodalFluxDensity = InterpolationHelper.InverseDistanceWeighting(node,
                    elementsNodeBelongsTo
                    .Select(e => (e.GetCentroid(), mapElementToFlux[e.Identifier]))
                    .ToList(), power: 3);
                int valuesStartIndex = _ResultMesh.MapNodeIndexToGlobalIndex[node.Index];
                values[valuesStartIndex * 3] = nodalFluxDensity.X;
                values[valuesStartIndex * 3 + 1] = nodalFluxDensity.Y;
                values[valuesStartIndex * 3 + 2] = nodalFluxDensity.Z;
                double abs = nodalFluxDensity.Magnitude();
            }
            return values;
        }
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
            return 2d*wmagTotal/Math.Pow(current, 2);*/
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
        }
    }
}