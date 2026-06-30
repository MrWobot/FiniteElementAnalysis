using Core.Maths;
using Core.Maths.Matrices;
using Core.Maths.Tensors;
using Core.Maths.Vectors;
using Core.Pool;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Results.Bases;

namespace FiniteElementAnalysis.Results
{
    public class StaticCurrentConductionResult : ScalarResultBase
    {
        public double[] NodalVoltages => CoreResult.UnknownsVector;
        public StaticCurrentConductionResult(IMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
        public double[]? GetVolumeCurrentDensityAtPoint(double[] point)
        {
            return _ResultMesh.GetElementsContainingPoint(point)
                    .Where(e => e.IsPointInside(point))
                    .Select(e => GetVolumeCurrentDensityForElement(e))
            .FirstOrDefault();
        }

        //CF working
        public VectorFieldResult GetNodalVolumeCurrentDensities(string fieldResultName)
        {
            return new VectorFieldResult(fieldResultName, GetNodalVolumeCurrentDensities());
        }
        public double GetAverageCurrentDensity()
        {
            double[] nodalCurrentDensities = GetNodalVolumeCurrentDensities();
            int i = 0;
            double sum = 0;
            while (i < nodalCurrentDensities.Length)
            {
                double sumSquares = 0;
                for (int j = 0; j < _ResultMesh.NodePositionLength; j++)
                {
                    sumSquares += Math.Pow(nodalCurrentDensities[i++], 2);
                }
                double magnitude = Math.Sqrt(sumSquares);
                sum += magnitude;
            }
            double nCurrentDensities = nodalCurrentDensities.Length / _ResultMesh.NodePositionLength;
            return sum / nCurrentDensities;
        }
        private double GetCurrentDensityMagnitude(double[] currentDensity)
        {
            return Math.Sqrt(currentDensity.Sum(v=>Math.Pow(v, 2)));
        }
        private double[] GetNodalVolumeCurrentDensities()
        {

            INode[] nodes = _ResultMesh.Nodes;
            double[] values = new double[nodes.Length * _ResultMesh.NodePositionLength];
            int valuesIndex = 0;
            foreach (INode node in nodes)
            {
                double[] currentDensity = GetVolumeCurrentDensityForNodeByAveragingElements(node.Identifier);
                for(int i=0; i<_ResultMesh.NodePositionLength; i++)
                {
                    values[valuesIndex++] = currentDensity[i];
                }
            }
            return values;
        }/* WRONG
        public VectorFieldResult GetNodalVolumetricCurrentDensities(string fieldName)
        {
            double[] nodalValues = new double[_ResultMesh.Nodes.Length * 3];
            GetNodalVolumetricCurrentDensities(_ResultMesh,
                new FieldDOFInfo(3, 3, FieldOperationType.Curl),
                null,
                (globalIndex, rhsElementValue) =>
                {

                    nodalValues[globalIndex] += rhsElementValue;
                });
            return new VectorFieldResult(
                fieldName,
                nodalValues);
        }
        */
        /// <summary>
        /// Multiplies the current density by the voluem of elements to 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="fieldDOFInfo"></param>
        /// <param name="K"></param>
        /// <param name="rhs"></param>
        /// <param name="operationIdentifier"></param>
        /// <param name="parentProgressHandler"></param>
        /// 
        private class NodalContributions
        {
            public int NContributions;
            public double[] Sum;
            public NodalContributions(double[] first)
            {
                NContributions = 1;
                Sum = new double[] { first[0], first[1], first[2] };
            }
        }
        /*working but not ideal. averages from elements*/
        
        public void ApplyVolumeCurrentDensities(
            IMesh meshBeingAppliedTo,
            FieldDOFInfo fieldDOFInfo,
            IBigMatrix K,
            double[] rhs,
            string operationIdentifier,
            CompositeProgressHandler? parentProgressHandler)
        {
            int nSpatialDims = _ResultMesh.NodePositionLength;
            double averageR0 = 0;
            foreach (INode thisNode in _ResultMesh.Nodes)
            {
                double[] total = new double[nSpatialDims];
                var elementsContainingNode = _ResultMesh.MapNodeToElementsBelongsTo[thisNode.Index];
                double[] totalForElements = new double[nSpatialDims];
                foreach (var element in elementsContainingNode)
                {
                    INode[] nodes = element.Nodes;
                    double conductivity = ((StaticCurrentVolume)element.VolumeBelongsTo!).Conductivity;
                    double[] voltages = nodes.Select(n => _MapNodeIndexToResultValue[n.Identifier]).ToArray();
                    int targetNodeIndex = Array.IndexOf(nodes, thisNode);
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        if (j == targetNodeIndex) continue;
                        double deltaV = voltages[j] - voltages[targetNodeIndex];
                        double[] dPosition = new double[nSpatialDims];
                        for (int k = 0; k < nSpatialDims; k++)
                            dPosition[k] = nodes[j].Position[k] - nodes[targetNodeIndex].Position[k];
                        double distance = Math.Sqrt(dPosition.Sum(v => v * v));
                        if (distance == 0) continue;
                        double scale = -deltaV / (distance * distance) * conductivity * element.Measure;
                        for (int k = 0; k < nSpatialDims; k++)
                            totalForElements[k] += dPosition[k] * scale;
                    }
                }
                for (int k = 0; k < nSpatialDims; k++)
                    total[k] += totalForElements[k] / elementsContainingNode.Count();
                int globalIndex = meshBeingAppliedTo.MapNodeIdentifierToGlobalIndex[thisNode.Identifier];
                averageR0 += total[0];
                for (int k = 0; k < nSpatialDims; k++)
                    rhs[globalIndex * nSpatialDims + k] = total[k];
            }
            averageR0 = averageR0 / _ResultMesh.Nodes.Length;
        }/*
        public void ApplyVolumeCurrentDensities(
        IMesh meshBeingAppliedTo,
        FieldDOFInfo fieldDOFInfo,
        IBigMatrix K,
        double[] rhs,
        string operationIdentifier,
        CompositeProgressHandler? parentProgressHandler)
        {
            double averageVolume = _ResultMesh.Elements.Select(e => e.Measure).Sum() / _ResultMesh.Elements.Length;

            double averageR0 = 0;
            foreach (Node thisNode in _ResultMesh.Nodes.Cast<Node>())
            {
                Vector3D total = new Vector3D(0, 0, 0);
                var elementsContainingNode =
                    _ResultMesh.MapNodeToElementsBelongsTo[thisNode.Identifier];
                Vector3D totalForElements = Vector3D.Zeros();
                foreach (var element in elementsContainingNode)
                {
                    Node[] nodes = element.Nodes.Cast<Node>().ToArray();
                    double conductivity = ((StaticCurrentVolume)element.VolumeBelongsTo!).Conductivity;





                    // Retrieve voltages at each node
                    double[] voltages = new double[] {
                    _MapNodeIndexToResultValue[nodes[0].Identifier],
                    _MapNodeIndexToResultValue[nodes[1].Identifier],
                    _MapNodeIndexToResultValue[nodes[2].Identifier],
                    _MapNodeIndexToResultValue[nodes[3].Identifier]
                };

                    int targetNodeIndex = Array.IndexOf(nodes, thisNode);
                    // Initialize the current density vector for the target node

                    // Loop through the other nodes to calculate their contributions to the target node
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        if (j == targetNodeIndex) continue; // Skip the target node itself

                        // Calculate the voltage difference ΔV between the target node and node j
                        double deltaV = voltages[j] - voltages[targetNodeIndex];

                        // Calculate the distance vector d between the target node and node j
                        Vector3D dPosition = nodes[j] - nodes[targetNodeIndex];
                        double distance = dPosition.Magnitude();

                        if (distance == 0) continue; // Skip if nodes overlap to avoid division by zero

                        // Calculate the unit direction vector from target node to node j
                        Vector3D unitDirection = dPosition.Normalize();

                        // Calculate the electric field contribution from node j to the target node
                        Vector3D electricFieldContribution = unitDirection.Scale(-deltaV / distance);

                        // Accumulate the current density contribution: J = σ * E
                        Vector3D currentDensityContribution = electricFieldContribution.Scale(conductivity);

                        // Add this to the total current density at the target node
                        totalForElements += currentDensityContribution.Scale(element.Measure);
                    }
                }
                total += totalForElements.Scale(1d / elementsContainingNode.Count());
                int globalIndex = meshBeingAppliedTo.MapNodeIdentifierToGlobalIndex[thisNode.Identifier];
                averageR0 += total.X;
                rhs[globalIndex * 3] = total.X;
                rhs[globalIndex * 3 + 1] = total.Y;
                rhs[globalIndex * 3 + 2] = total.Z;
            }
            averageR0 = averageR0 / _ResultMesh.Nodes.Length;
        }
        
        public void ApplyVolumeCurrentDensitiesProperUntested(
            IMesh meshBeingAppliedTo,
            FieldDOFInfo fieldDOFInfo,
            IBigMatrix K,
            double[] rhs,
            string operationIdentifier,
            CompositeProgressHandler? parentProgressHandler)
        {
            int nSpatialDims = _ResultMesh.NodePositionLength;
            foreach (INode node in meshBeingAppliedTo.Nodes)
            {
                var elementsContainingNode = _ResultMesh.MapNodeToElementsBelongsTo[node.Identifier];
                double[] weightedCurrentDensity = new double[nSpatialDims];
                double totalVolume = 0;
                foreach (var element in elementsContainingNode)
                {
                    double[] currentDensity = GetVolumeCurrentDensityForElement(element);
                    double elementVolume = element.Measure;
                    for (int k = 0; k < nSpatialDims; k++)
                        weightedCurrentDensity[k] += currentDensity[k] * elementVolume;
                    totalVolume += elementVolume;
                }
                int globalIndex = meshBeingAppliedTo.MapNodeIdentifierToGlobalIndex[node.Identifier];
                for (int k = 0; k < nSpatialDims; k++)
                    rhs[globalIndex * nSpatialDims + k] = weightedCurrentDensity[k] / totalVolume;
            }
        }
        /*
        public double[] ComputeInflowOutflowForElementWithShapeFunctions(TetrahedronElement element, double conductivity)
        {
            // Element volume
            double elementVolume = element.ElementVolume;

            // Nodal voltages (computed from static current conduction analysis)
            double[] voltages = new double[] {
            _MapNodeIndexToResultValue[element.NodeA.Index],
            _MapNodeIndexToResultValue[element.NodeB.Index],
            _MapNodeIndexToResultValue[element.NodeC.Index],
            _MapNodeIndexToResultValue[element.NodeD.Index]
    };

            // Gradient of the shape functions (B matrix)
            double[][] BMatrix = element.ScalarBMatrix;

            // Shape functions matrix (N) for each node in the element
            double[][] shapeFunctions = element.ShapeFunctionsMatrixN;

            // Array to hold the total current density (x, y, z) for the node
            double[] totalCurrentDensity = new double[3];

            // Loop through each node in the element
            for (int i = 0; i < element.Nodes.Length; i++)
            {
                var thisNode = element.Nodes[i];
                double thisNodePotential = voltages[i];

                // Loop through other nodes to compute the inflow/outflow contribution
                for (int j = 0; j < element.Nodes.Length; j++)
                {
                    if (i == j) continue; // Skip self-contribution

                    // Voltage difference between nodes
                    double dV = voltages[j] - thisNodePotential;

                    // Gradient of shape functions (B matrix) for the other node
                    double bGradX = BMatrix[0][j]; // Gradient in x
                    double bGradY = BMatrix[1][j]; // Gradient in y
                    double bGradZ = BMatrix[2][j]; // Gradient in z

                    // Current density contributions (J = -σ * ∇V) using voltage gradients
                    double currentDensityX = -conductivity * dV * bGradX;
                    double currentDensityY = -conductivity * dV * bGradY;
                    double currentDensityZ = -conductivity * dV * bGradZ;

                    // Adjust the current density contributions using the shape functions
                    // The shape function ensures that the current density is correctly weighted based on the geometry
                    double shapeFuncX = shapeFunctions[0][i]; // Shape function value for x at node i
                    double shapeFuncY = shapeFunctions[1][i]; // Shape function value for y at node i
                    double shapeFuncZ = shapeFunctions[2][i]; // Shape function value for z at node i

                    // Sign adjustment based on relative positions (to account for inflow/outflow)
                    double[] elementNodePosition = element.Nodes[j].Position;
                    double[] thisNodePosition = thisNode.Position;
                    double xSign = Math.Sign(elementNodePosition[0] - thisNodePosition[0]);
                    double ySign = Math.Sign(elementNodePosition[1] - thisNodePosition[1]);
                    double zSign = Math.Sign(elementNodePosition[2] - thisNodePosition[2]);

                    // Sum the contributions for inflow/outflow, weighted by the shape functions
                    totalCurrentDensity[0] += currentDensityX * shapeFuncX * xSign;
                    totalCurrentDensity[1] += currentDensityY * shapeFuncY * ySign;
                    totalCurrentDensity[2] += currentDensityZ * shapeFuncZ * zSign;
                }
            }

            // Scale by the element volume to convert to Amp-meters (A·m)
            totalCurrentDensity[0] *= elementVolume / 2.0; // Divide by 2 to account for shared contributions
            totalCurrentDensity[1] *= elementVolume / 2.0;
            totalCurrentDensity[2] *= elementVolume / 2.0;

            return totalCurrentDensity; // This represents the inflow/outflow current density with correct units (A·m)
        }*/
        /*the one working on
        public void ApplyVolumeCurrentDensities(
    TetrahedralMesh meshBeingAppliedTo,
    FieldDOFInfo fieldDOFInfo,
    IBigMatrix K,
    double[] rhs,
    string operationIdentifier,
    CompositeProgressHandler? parentProgressHandler)
{
    foreach (var element in _ResultMesh.Elements)
    {
        double elementVolume = element.ElementVolume;

        // Retrieve the nodal voltages from static current conduction for this element
        double[] V = new double[] {
            _MapNodeIdentifierToResultValue[element.NodeA.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeB.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeC.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeD.Identifier]
        };

        // Use the scalar B matrix (gradient) to compute the gradient of voltages (i.e., the current density)
        double[] gradV = MatrixHelper.MatrixMultipliedByVector(element.ScalarBMatrix, V);
                double[] nodalGradVsX = VectorHelper.Multiply(element.ScalarBMatrix[0], V);
                double[] nodalGradVsY = VectorHelper.Multiply(element.ScalarBMatrix[1], V);
                double[] nodalGradVsZ = VectorHelper.Multiply(element.ScalarBMatrix[2], V);
                double[] newGradV = new double[] { nodalGradVsX .Sum(), nodalGradVsY.Sum(), nodalGradVsZ.Sum()};
                double conductivity = ((StaticCurrentVolume)element.VolumeIsAPartOf!).Conductivity;
                double[] currentDensityXComponentsFlowingThroughEachNode = VectorHelper.Scale(nodalGradVsX, conductivity);
                double[] currentDensityYComponentsFlowingThroughEachNode = VectorHelper.Scale(nodalGradVsY, conductivity);
                double[] currentDensityZComponentsFlowingThroughEachNode = VectorHelper.Scale(nodalGradVsZ, conductivity);
                double currentDensityX = currentDensityXComponentsFlowingThroughEachNode.Sum();//...get it
                var test = ComputeInflowOutflowForElementWithShapeFunctions(element, conductivity);
                //check gpt for the last message i sent which explains rest of what i need to do. get the sign corresponding to the direciton, sum these all then divide by two.
                // Compute the current density vector J = -σ * grad(V)
        double[] currentDensity = VectorHelper.Scale(gradV, -conductivity);

        // Now multiply with the shape functions N^T and the volume
        double[] N_T_J = MatrixHelper.MatrixMultipliedByVector(MatrixHelper.MatrixTranspose(element.ShapeFunctionsMatrixN), currentDensity);

        // Scale the result by the element volume
        double[] fe = VectorHelper.Scale(N_T_J, elementVolume);

        // Stamp fe into the global rhs vector
        int feIndex = 0;
        foreach (var elementNode in element.Nodes)
        {
            int globalIndex = meshBeingAppliedTo.MapNodeIdentifierToGlobalIndex[elementNode.Identifier];
            int rhsIndex = globalIndex * 3;

            rhs[rhsIndex++] += fe[feIndex++]; // X component
            rhs[rhsIndex++] += fe[feIndex++]; // Y component
            rhs[rhsIndex++] += fe[feIndex++]; // Z component
        }
    }
}
        */
        /* his attempt to expand which didnt work
        public void ApplyVolumeCurrentDensities(
  TetrahedralMesh meshBeingAppliedTo,
  FieldDOFInfo fieldDOFInfo,
  IBigMatrix K,
  double[] rhs,
  string operationIdentifier,
  CompositeProgressHandler? parentProgressHandler)
        {
            foreach (var element in _ResultMesh.Elements)
            {
                double elementVolume = element.ElementVolume;

                // Retrieve the nodal voltages from static current conduction for this element
                double[] V = new double[] {
            _MapNodeIdentifierToResultValue[element.NodeA.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeB.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeC.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeD.Identifier]
        };

                // Manually define the shape function derivatives (B matrix gradients)
                double[][] BScalar = element.ScalarBMatrix;

                // Get the positions of the nodes in the element
                double[][] nodePositions = new double[][]
                {
            new double[] { element.NodeA.X, element.NodeA.Y, element.NodeA.Z },
            new double[] { element.NodeB.X, element.NodeB.Y, element.NodeB.Z },
            new double[] { element.NodeC.X, element.NodeC.Y, element.NodeC.Z },
            new double[] { element.NodeD.X, element.NodeD.Y, element.NodeD.Z }
                };

                // Loop through each node in the element
                for (int i = 0; i < element.Nodes.Length; i++)
                {
                    var thisNode = element.Nodes[i];
                    double[] thisNodePosition = nodePositions[i];
                    double thisNodePotential = V[i];

                    int globalIndex = meshBeingAppliedTo.MapNodeIdentifierToGlobalIndex[thisNode.Identifier];
                    int rhsIndex = globalIndex * 3;

                    double[] totalCurrentDensity = new double[3]; // x, y, z components of current density for this node

                    // Calculate the current density contributions using the B matrix gradients
                    for (int j = 0; j < element.Nodes.Length; j++)
                    {
                        if (i == j) continue; // Skip self-contribution

                        // Retrieve the voltage difference and corresponding shape function gradient values (from B matrix)
                        double dV = V[j] - thisNodePotential;
                        double bGrad = BScalar[0][j];  // ∂N/∂x for node j
                        double cGrad = BScalar[1][j];  // ∂N/∂y for node j
                        double dGrad = BScalar[2][j];  // ∂N/∂z for node j

                        // Compute the current density contribution
                        double conductivity = ((StaticCurrentVolume)element.VolumeIsAPartOf!).Conductivity;
                        double currentDensityX = -conductivity * (dV * bGrad); // Contribution to x-direction current density
                        double currentDensityY = -conductivity * (dV * cGrad); // Contribution to y-direction current density
                        double currentDensityZ = -conductivity * (dV * dGrad); // Contribution to z-direction current density

                        // **Shape function application**:
                        // Now multiply with the shape functions to distribute current density contributions
                        double shapeFuncX = element.ShapeFunctionsMatrixN[0][i]; // shape function value for x at node i
                        double shapeFuncY = element.ShapeFunctionsMatrixN[1][i]; // shape function value for y at node i
                        double shapeFuncZ = element.ShapeFunctionsMatrixN[2][i]; // shape function value for z at node i

                        // Sum the contributions for this node, weighted by the shape functions
                        totalCurrentDensity[0] += currentDensityX * shapeFuncX;
                        totalCurrentDensity[1] += currentDensityY * shapeFuncY;
                        totalCurrentDensity[2] += currentDensityZ * shapeFuncZ;
                    }

                    // Scale the result by the element volume
                    totalCurrentDensity[0] *= elementVolume;
                    totalCurrentDensity[1] *= elementVolume;
                    totalCurrentDensity[2] *= elementVolume;

                    // Stamp the total current density contribution into the global rhs vector
                    rhs[rhsIndex++] += totalCurrentDensity[0]; // X component
                    rhs[rhsIndex++] += totalCurrentDensity[1]; // Y component
                    rhs[rhsIndex++] += totalCurrentDensity[2]; // Z component
                }
            }
        }

        /*my attempt to build on not working expansion to use directions. 
        public void ApplyVolumeCurrentDensities(
  TetrahedralMesh meshBeingAppliedTo,
  FieldDOFInfo fieldDOFInfo,
  IBigMatrix K,
  double[] rhs,
  string operationIdentifier,
  CompositeProgressHandler? parentProgressHandler)
        {
            foreach (var element in _ResultMesh.Elements)
            {
                double elementVolume = element.ElementVolume;

                // Retrieve the nodal voltages from static current conduction for this element
                double[] V = new double[] {
            _MapNodeIdentifierToResultValue[element.NodeA.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeB.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeC.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeD.Identifier]
        };

                // Manually define the shape function derivatives (B matrix gradients)
                double[][] shapeFunctions = element.ShapeFunctionsMatrixN;

                // Extract gradient values (same as B matrix gradient approach)
                double[][] BScalar = element.ScalarBMatrix;

                // Get the positions of the nodes in the element
                double[][] nodePositions = new double[][]
                {
            new double[] { element.NodeA.X, element.NodeA.Y, element.NodeA.Z },
            new double[] { element.NodeB.X, element.NodeB.Y, element.NodeB.Z },
            new double[] { element.NodeC.X, element.NodeC.Y, element.NodeC.Z },
            new double[] { element.NodeD.X, element.NodeD.Y, element.NodeD.Z }
                };

                // Loop through each node in the element
                for (int i = 0; i < element.Nodes.Length; i++)
                {
                    var thisNode = element.Nodes[i];
                    double[] thisNodePosition = nodePositions[i];
                    double thisNodePotential = V[i];


                    double[] totalCurrentDensity = new double[3]; // x, y, z components of current density for this node

                    // Calculate the current density contributions using the B matrix gradients
                    for (int j = 0; j < element.Nodes.Length; j++)
                    {
                        if (i == j) continue; // Skip self-contribution
                        Node otherNode = element.Nodes[j];
                        // Retrieve the voltage difference and corresponding shape function gradient values (from B matrix)
                        double dV = V[j] - thisNodePotential;
                        double bGrad = BScalar[0][j];  // ∂N/∂x for node j
                        double cGrad = BScalar[1][j];  // ∂N/∂y for node j
                        double dGrad = BScalar[2][j];  // ∂N/∂z for node j
                        double xSign = Math.Sign(thisNode.X - otherNode.X);
                        double ySign = Math.Sign(thisNode.Y - otherNode.Y);
                        double zSign = Math.Sign(thisNode.Z - otherNode.Z);
                        // Compute the current density contribution
                        double conductivity = ((StaticCurrentVolume)element.VolumeIsAPartOf!).Conductivity;

                        double currentDensityX = -conductivity * (dV * bGrad)*xSign; // Contribution to x-direction current density
                        double currentDensityY = -conductivity * (dV * cGrad)*ySign; // Contribution to y-direction current density
                        double currentDensityZ = -conductivity * (dV * dGrad)*zSign; // Contribution to z-direction current density

                        // Sum the contributions for this node
                        totalCurrentDensity[0] += currentDensityX;
                        totalCurrentDensity[1] += currentDensityY;
                        totalCurrentDensity[2] += currentDensityZ;
                    }

                    // Scale the result by the element volume
                    totalCurrentDensity[0] *= elementVolume/2d;
                    totalCurrentDensity[1] *= elementVolume/2d;
                    totalCurrentDensity[2] *= elementVolume/2d;

                    // Stamp the total current density contribution into the global rhs vector
                    int globalIndex = meshBeingAppliedTo.MapNodeIdentifierToGlobalIndex[thisNode.Identifier];
                    int rhsIndex = globalIndex * 3;
                    rhs[rhsIndex++] += totalCurrentDensity[0]; // X component
                    rhs[rhsIndex++] += totalCurrentDensity[1]; // Y component
                    rhs[rhsIndex++] += totalCurrentDensity[2]; // Z component
                }
            }
        }*/

        //CF working
        private double[] GetVolumeCurrentDensityForElement(IElement element)
        {
            double[] V = element.Nodes.Select(n => _MapNodeIndexToResultValue[n.Identifier]).ToArray();
            return VectorHelper.Scale(
                MatrixHelper.MatrixMultiplyByVector(
                    //Should return ScalarBMatrix. 
                    element.GetBMatrix(CoreResult.NFieldComponents, CoreResult.FieldOperationType, CoreResult.NDegreesOfFreedom), V),
                -1 * ((StaticCurrentVolume)element.VolumeBelongsTo!).Conductivity);
        }



        //CF working
        private double[] GetVolumeCurrentDensityForNodeByAveragingElements(int nodeIdentifier)
        {
            int nSpatialDims = _ResultMesh.NodePositionLength;
            double[] totalWeightedCurrentDensity = new double[nSpatialDims];
            if (!_ResultMesh.MapNodeToElementsBelongsTo.TryGetValue(nodeIdentifier,
                out List<IElement>? elementsContainingNode))
            {
                return totalWeightedCurrentDensity;
            }
            double totalVolume = 0;
            foreach (var element in elementsContainingNode)
            {
                double elementVolume = element.Measure;
                double[] elementCurrentDensity = GetVolumeCurrentDensityForElement(element);
                for (int i = 0; i < nSpatialDims; i++)
                    totalWeightedCurrentDensity[i] += elementCurrentDensity[i] * elementVolume;
                totalVolume += elementVolume;
            }
            return totalWeightedCurrentDensity.Select(v => v / totalVolume).ToArray();
        }

        /*
        private void GetNodalVolumetricCurrentDensities(
            TetrahedralMesh meshApplyingTo,
            FieldDOFInfo fieldDOFInfo,
            CompositeProgressHandler? parentProgressHandler,
            Action<int, double> apply)
        {
            if (fieldDOFInfo.NFieldComponents != 3 || fieldDOFInfo.NDegreesOfFreedom != 3)
            {
                throw new NotImplementedException("Only implemented for 3 field components and 3 degrees of freedom");
            }
            TetrahedronElement[] elementsApplyingTo = meshApplyingTo.Elements;
            StandardProgressHandler? progressHandler = null;
            Action? updateProgress = null;
            if (parentProgressHandler != null)
            {
                progressHandler = new StandardProgressHandler();
                parentProgressHandler.AddChild(progressHandler);
                if (elementsApplyingTo.Length < 1)
                {
                    progressHandler.Set(1);
                    return;
                }
                updateProgress = progressHandler.GetUpdateProgress(elementsApplyingTo.Length, elementsApplyingTo.Length > 100 ? elementsApplyingTo.Length / 100 : elementsApplyingTo.Length);
            }
            foreach (TetrahedronElement elementApplyingTo in elementsApplyingTo)
            {
                double cubeRootVolume = Math.Pow(elementApplyingTo.ElementVolume, 1d / 3d);
                if (!_ResultMesh.MapElementIdentifierToElement.TryGetValue(
                        elementApplyingTo.Identifier, out TetrahedronElement? myElement))
                {
                    continue;
                }
                double[] elementCurrentDensitiesAtNodes =
                    GetVolumetricCurrentDensityFromElement(myElement
                    ).ToArray();
                double[][] elementBTranspose =
                    elementApplyingTo.GetBMatrixTranspose(fieldDOFInfo);
                double[] rhsE = MatrixHelper.BlockMatrixMultipliedByVector(
                    elementBTranspose,
                    elementCurrentDensitiesAtNodes);
                if (rhsE.Length != 12) throw new Exception("Dimension mismatch");
                int rhsEIndex = 0;
                for (int elementNodeIndex = 0; elementNodeIndex < 4; elementNodeIndex++)
                {
                    Node node = elementApplyingTo.Nodes[elementNodeIndex];
                    int nodeIndex = meshApplyingTo.MapNodeIdentifierToGlobalIndex[node];
                    int globalIndexStart = nodeIndex * fieldDOFInfo.NDegreesOfFreedom;
                    for (int i = 0; i < fieldDOFInfo.NDegreesOfFreedom; i++)
                    {
                        apply(globalIndexStart++, rhsE[rhsEIndex++]);
                    }
                }
                updateProgress?.Invoke();
            }
            progressHandler?.Set(1);
        }*/
        /* new
        public void ApplyVolumetricCurrentDensities(
            TetrahedralMesh meshApplyingTo, 
            FieldDOFInfo fieldDOFInfo,
            IBigMatrix K,
            double[] rhs, 
            string operationIdentifier,
            Dictionary<Node, int> mapNodeToGlobalIndex)
        {
            if (fieldDOFInfo.NFieldComponents != 3||fieldDOFInfo.NDegreesOfFreedom!=3) {
                throw new NotImplementedException("Only implemented for 3 field components and 3 degrees of freedom");
            }
            foreach (Node nodeInMeshApplyingTo in meshApplyingTo.Nodes)
            {
                int nodeIdentifier = nodeInMeshApplyingTo.Identifier;
                var sourceMesh = _MapTetrahedralMeshToMapNodeToVoltage.Where(kvp =>
                kvp.Value.ContainsKey(nodeIdentifier)).Select(kvp=>kvp.Key).FirstOrDefault();
                if(sourceMesh == null) { 
                    continue; 
                }
                var mapNodeToVoltage = _MapTetrahedralMeshToMapNodeToVoltage[sourceMesh];
                var elementsBelongsTo = sourceMesh.MapNodeToElementsBelongsTo[nodeInMeshApplyingTo.Identifier];
                var adjacentNodeWithAverageElementConductivitys = elementsBelongsTo
                    .SelectMany(e => e.Nodes.
                        Where(n => n.Identifier != nodeIdentifier)
                        .Where(n => mapNodeToVoltage.ContainsKey(n.Identifier))
                        .Select(n => new { element = e, node = n })
                    .GroupBy(o => o.node.Identifier)
                    .Select(g => new
                    {
                        node = g.First().node,
                        elementsInStaticCurrentVolume = g
                        .Where(o => typeof(StaticCurrentVolume)
                            .IsAssignableFrom(o.element.VolumeIsAPartOf!.GetType()))
                        .Select(o=>o.element)
                    })
                    .Where(o=> o.elementsInStaticCurrentVolume != null 
                        && o.elementsInStaticCurrentVolume.Count() >0
                    )
                    .Select(o =>
                    {
                        return new
                        {
                            node = o.node,
                            averageConductivity =
                            o.elementsInStaticCurrentVolume
                            .Select(e => 
                                ((StaticCurrentVolume)e.VolumeIsAPartOf!).Conductivity 
                                * e.ElementVolume)
                            .Sum()
                            / o.elementsInStaticCurrentVolume.Select(e=>e.ElementVolume).Sum()
                        };
                    })
                    )
                    .ToArray();
                double voltageAtNode = mapNodeToVoltage[nodeIdentifier];
                Vector3D currentDensity = new Vector3D(0, 0, 0);
                foreach (var adjacentNodeWithAverageElementConductivity in adjacentNodeWithAverageElementConductivitys) {
                    Node adjacentNode = adjacentNodeWithAverageElementConductivity.node;
                    int adjacentNodeIdentifier = adjacentNode.Identifier;
                    double voltageAtAdjacentNode = mapNodeToVoltage[adjacentNodeIdentifier];
                    double deltaV = voltageAtNode - voltageAtAdjacentNode;
                    Vector3D distance = nodeInMeshApplyingTo - adjacentNode;
                    double distanceMagnitude = distance.Magnitude();
                    Vector3D electricField = distance.Normalize().Scale(deltaV / distanceMagnitude);
                    currentDensity += electricField.Scale(adjacentNodeWithAverageElementConductivity.averageConductivity);
                }

                int nodeIndex = mapNodeToGlobalIndex[nodeInMeshApplyingTo];
                int globalIndexStart = nodeIndex * fieldDOFInfo.NDegreesOfFreedom;
                rhs[globalIndexStart++] += currentDensity.X;
                rhs[globalIndexStart++] += currentDensity.Y;
                rhs[globalIndexStart++] += currentDensity.Z;
            }
        }*/
    }
}