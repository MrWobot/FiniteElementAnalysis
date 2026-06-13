using Core.Maths;
using Core.Maths.Tensors;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Interpolation;
using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;

namespace FiniteElementAnalysis.Results
{
    public class StaticMagneticConductionResult : VectorResultBase
    {
        public double[] NodalMagneticVectorPotentials => CoreResult.UnknownsVector;
        public StaticMagneticConductionResult(TetrahedralMesh mesh, CoreSolverResult coreResult)
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
        private Vector3D GetElementMagneticFluxDensity(TetrahedronElement element) {

            double[] nodalMagneticVectorPotentials =
                element.Nodes.SelectMany(n => _MapNodeIdentifierToResultValue[n.Identifier])
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
            double magneticFlux =  CalculateMagneticFlux(nPlanesToDivideBy, out double ignore, measurementBoundaries);
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
        public double CalculateMagneticFlux(int nPlanesToDivideBy, out double totalArea, params MeasurementBoundary[] measurementBoundaries) {
            double totalFluxLinkage = 0;
            totalArea = 0;
            foreach (var measurementBoundary in measurementBoundaries)
            {
                var faces = _ResultMesh.GetFacesForBoundary(measurementBoundary);
                if (faces == null) throw new Exception($"No faces for boundary named\"{measurementBoundary.Name}\"");
                foreach (BoundaryFace face in faces) {
                    var faceElementInterstedIn = face.Elements.Where(e => (e.GetCentroid() - face.CenterPoint).Dot(face.Normal) >= 0);
                    if(faceElementInterstedIn.Count() > 1) {
                        throw new Exception("Something went wrong");
                    }
                    TetrahedronElement elementOneSideOfFace = faceElementInterstedIn.First();
                    Vector3D fluxDensity = GetElementMagneticFluxDensity(elementOneSideOfFace);
                    Vector3D unitDirectionFace = face.Normal;
                    double fluxNormalToFace = fluxDensity.Dot(unitDirectionFace);
                    double faceArea = face.Area;
                    double fluxLinkageForFace = Math.Abs(fluxNormalToFace*faceArea);
                    totalFluxLinkage += fluxLinkageForFace;
                    totalArea += faceArea;
                }
            }
            totalArea /= (double)nPlanesToDivideBy;
            return totalFluxLinkage/(double)nPlanesToDivideBy;
        }
        public double[] GetNodalMagneticFluxDensityB()
        {
            double[] values = new double[_ResultMesh.Nodes.Length * 3];
            Dictionary<int, Vector3D> mapElementToFlux = new Dictionary<int, Vector3D>();
            foreach (var element in _ResultMesh.Elements) {
                mapElementToFlux[element.Identifier] = GetElementMagneticFluxDensity(element);
            }
            foreach (Node node in _ResultMesh.Nodes) {
                var elementsNodeBelongsTo = _ResultMesh.MapNodeToElementsBelongsTo[node.Identifier];
                Vector3D nodalFluxDensity = InterpolationHelper.InverseDistanceWeighting(node,
                    elementsNodeBelongsTo
                    .Select(e => (e.GetCentroid(), mapElementToFlux[e.Identifier]))
                    .ToList(), power: 3);
                int valuesStartIndex = _ResultMesh.MapNodeIdentifierToGlobalIndex[node.Identifier];
                values[valuesStartIndex * 3] = nodalFluxDensity.X;
                values[(valuesStartIndex * 3) + 1] = nodalFluxDensity.Y;
                values[(valuesStartIndex * 3) + 2] = nodalFluxDensity.Z;
                double abs = nodalFluxDensity.Magnitude();
            }
            return values;
            // Dictionary to store total weighted flux density sum at each node
            Dictionary<int, Vector3D> totalFluxDensityAtNode = new Dictionary<int, Vector3D>();
            Dictionary<int, double> totalAreaAtNode = new Dictionary<int, double>();

            // Loop through each element
            foreach (var element in _ResultMesh.Elements)
            {
                Vector3D elementB = GetElementMagneticFluxDensity(element); // Get the element's B vector

                // Loop through each node in the tetrahedron element
                foreach (var node in element.Nodes)
                {
                    // Calculate the face opposite to the node (3 remaining nodes)
                    TriangleElementFace face = element.GetFaceOppositeNode(node.Identifier);
                    double faceArea = face.Area;

                    // Project the area perpendicular to B for accurate weighting
                    Vector3D faceNormal = face.Normal;
                    double projectedArea = faceArea * Math.Abs(faceNormal.Dot(elementB.Normalize()));

                    // Calculate weighted B contribution for the node
                    Vector3D weightedB = elementB.Scale(projectedArea);

                    // Accumulate the weighted B and area at the node
                    if (!totalFluxDensityAtNode.ContainsKey(node.Identifier))
                    {
                        totalFluxDensityAtNode[node.Identifier] = weightedB;
                        totalAreaAtNode[node.Identifier] = projectedArea;
                    }
                    else
                    {
                        totalFluxDensityAtNode[node.Identifier] += weightedB;
                        totalAreaAtNode[node.Identifier] += projectedArea;
                    }
                }
            }

            // Calculate final nodal flux density by normalizing by total area
            HashSet<int> seenIndices = new HashSet<int>();
            for (int i = 0; i < _ResultMesh.Nodes.Length; i++) {
                seenIndices.Add(i);
            }
            foreach (var nodeIdentifier in totalFluxDensityAtNode.Keys)
            {
                Vector3D nodalFluxDensity = totalFluxDensityAtNode[nodeIdentifier].Scale(1.0d / totalAreaAtNode[nodeIdentifier]);
                int valuesStartIndex = _ResultMesh.MapNodeIdentifierToGlobalIndex[nodeIdentifier];
                if(!seenIndices.Contains(valuesStartIndex))
                {

                }
                seenIndices.Remove(valuesStartIndex);
                values[valuesStartIndex * 3] = nodalFluxDensity.X;
                values[(valuesStartIndex * 3) + 1] = nodalFluxDensity.Y;
                values[(valuesStartIndex * 3) + 2] = nodalFluxDensity.Z;
            }
            return values;
            /*
            Dictionary<int, double[]> mapNodeToB_iSum = new Dictionary<int, double[]>();
            Dictionary<int, double> mapNodeToNComponents= new Dictionary<int, double>();
            foreach (TetrahedronElement element in _ResultMesh.Elements)
            {
                double[][] shapeFunctions = element.ShapeFunctions;
                for(int elementNodeIndex = 0; elementNodeIndex < 4; elementNodeIndex++) {
                    Node node = element.Nodes[elementNodeIndex];
                    double[] shapeFunction = shapeFunctions[elementNodeIndex];
                    double shapeFunctionValueAtNode = shapeFunction[0] 
                        + (shapeFunction[1] * node.X) 
                        + (shapeFunction[2] * node.Y) 
                        + (shapeFunction[3] * node.Z);
                    double[] N_iA_i = VectorHelper.Scale(
                        _MapNodeIdentifierToResultValue[node.Identifier],
                        shapeFunctionValueAtNode);
                    double N_iA_ix = N_iA_i[0];
                    double N_iA_iy = N_iA_i[1];
                    double N_iA_iz = N_iA_i[2];
                    double a = shapeFunction[0];
                    double b = shapeFunction[1];
                    double c = shapeFunction[2];
                    double d = shapeFunction[3];
                    double B_ix = (c * N_iA_iz) - (d * N_iA_iy);
                    double B_iy = (d * N_iA_ix) - (b * N_iA_iz);
                    double B_iz = (b * N_iA_iy) - (c * N_iA_ix);
                    double[] B_i = new double[] { B_ix, B_iy, B_iz };
                    double[] B_iScaled = B_i;
                    int nodeIdentifier = node.Identifier;
                    if (mapNodeToB_iSum.TryGetValue(nodeIdentifier, out double[]? B_iSum))
                    {
                        B_iSum[0] = B_iSum[0] + B_iScaled[0];
                        B_iSum[1] = B_iSum[1] + B_iScaled[1];
                        B_iSum[2] = B_iSum[2] + B_iScaled[2];
                        mapNodeToNComponents[nodeIdentifier] = mapNodeToNComponents[nodeIdentifier] + 1;
                    }
                    else {
                        mapNodeToB_iSum[nodeIdentifier] = B_iScaled;
                        mapNodeToNComponents[nodeIdentifier] = 1;
                    }
                }
            }
            double[] values = new double[_ResultMesh.Nodes.Length * 3];
            int nodeIndex = 0;
            double largestMagnitude = 0;
            int testCount = 0;
            foreach (Node node in _ResultMesh.Nodes)
            {
                double[] B_isum = VectorHelper.Scale(mapNodeToB_iSum[node.Identifier], 1d/mapNodeToNComponents[node.Identifier]);
                Console.WriteLine($"[{B_isum[0]},{B_isum[1]},{B_isum[2]}]");
                values[nodeIndex * 3] = B_isum[0];
                values[(nodeIndex * 3)+1] = B_isum[1];
                values[(nodeIndex * 3)+2] = B_isum[2];
                nodeIndex++;
                double magnitude = Math.Sqrt(Math.Pow(B_isum[0], 2) + Math.Pow(B_isum[1], 2) + Math.Pow(B_isum[2], 2));
                Console.WriteLine($"|B|={magnitude}");
                if (magnitude > largestMagnitude)
                    largestMagnitude = magnitude;
                if (magnitude > 1) {
                    testCount++;
                }
            }
            Console.WriteLine($"|Bmax|={largestMagnitude}");
            return values;

            // Cache to avoid recalculating element magnetic flux for the same element
            TetrahedralMesh forMesh = _ResultMesh;
            Dictionary<int, double[]> cacheElementMagneticFluxBs
                = new Dictionary<int, double[]>();
            Func<TetrahedronElement, double[]> getElementMagneticFluxB = (sourceElement) =>
            {
                // Check if magnetic flux B for the element is already cached
                if (cacheElementMagneticFluxBs.TryGetValue(
                    sourceElement.Identifier, out double[]? magneticFluxB))
                {
                    return magneticFluxB!;
                }
                if (sourceElement.NodalValuesAsVector.Length != 12)
                {
                    throw new Exception("Sanity check");
                }
                // Calculate magnetic flux B using BMatrix and nodal values
                magneticFluxB = MatrixHelper.MatrixMultipliedByVector(
                    sourceElement.BMatrix3DOF3FieldComponentsUsingCurl,
                    sourceElement.NodalValuesAsVector
                );

                // Cache the calculated magnetic flux B for the element
                cacheElementMagneticFluxBs[sourceElement.Identifier] = magneticFluxB;
                return magneticFluxB;
            };
            var core = CoreResult;
            Node[] forNodes = forMesh.Nodes;
            double[] results = new double[forNodes.Length * 3]; // 3 values (x, y, z) for each node
            double[] volumeContributions = new double[forNodes.Length]; // To track the number of elements contributing to each node

            int resultIndex = 0;
            for (int forNodeIndex = 0; forNodeIndex < forNodes.Length; forNodeIndex++)
            {
                Node forNode = forNodes[forNodeIndex];
                var elementsNodeBelongsTo = GetElementsNodeBelongsTo(forNode.Identifier);
                foreach (var sourceElement in elementsNodeBelongsTo)
                {
                    double[] magneticFluxForElement = getElementMagneticFluxB(sourceElement);
                    double elementVolume = sourceElement.ElementVolume;
                    // Accumulate magnetic flux contributions for each node
                    results[resultIndex] += magneticFluxForElement[0]*elementVolume;
                    results[resultIndex + 1] += magneticFluxForElement[1] * elementVolume;
                    results[resultIndex + 2] += magneticFluxForElement[2] * elementVolume;

                    volumeContributions[forNodeIndex]+= elementVolume; // Track contributions for averaging
                }
                resultIndex += 3; // Move to the next node's results (3 values per node)
            }

            // Step to average the magnetic flux at each node
            resultIndex = 0;
            int countContributionsIndex = 0;
            while (resultIndex < forNodes.Length)
            {
                if (volumeContributions[countContributionsIndex] <= 0)
                {
                    resultIndex += 3;
                }
                else
                {
                    double totalVolume = (double)volumeContributions[countContributionsIndex];
                    results[resultIndex] /= totalVolume; // Average Bx
                    resultIndex++;
                    results[resultIndex] /= totalVolume; // Average By
                    resultIndex++;
                    results[resultIndex] /= totalVolume; // Average Bz
                    resultIndex++;
                }
                countContributionsIndex++;
            }

            return results; // Return the array containing 3 values for each node (Bx, By, Bz)*/
        }
        public double CalculateWindingSelfInductance(
            double current,
            TetrahedralMesh forMesh,
            StaticCurrentConductionResult currentDensitiesForWindingMesh)
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
                int nodeGlobalIndex = forMesh.MapNodeIdentifierToGlobalIndex[node.Identifier];
                double[] magneticVectorPotential = mapNodeToMagneticVectorPotential[node.Identifier];
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