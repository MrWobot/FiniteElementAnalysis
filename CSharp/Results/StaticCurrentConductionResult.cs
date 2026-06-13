using Core.Maths;
using Core.Maths.Matrices;
using Core.Maths.Tensors;
using Core.Maths.Vectors;
using Core.Pool;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;

namespace FiniteElementAnalysis.Results
{
    public class StaticCurrentConductionResult : ScalarResultBase
    {
        public double[] NodalVoltages => CoreResult.UnknownsVector;
        public StaticCurrentConductionResult(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
        public Vector3D? GetVolumeCurrentDensityAtPoint(Vector3D point)
        {
            return _ResultMesh.ElementsBVHTree.QueryBVH(point)
                    .Where(e => e.IsPointInside(point))
                    .Select(e => GetVolumeCurrentDensityForElement(e))
            .FirstOrDefault();
        }// Function to compute the current density at a point within an element
        public double[] ComputeCurrentDensityAtPoint(TetrahedronElement element, Vector3D point)
        {
            double[] currentDensity = new double[3]; // J_x, J_y, J_z
            double[] electricField = new double[3];  // E_x, E_y, E_z

            // Loop through the 4 nodes of the tetrahedral element
            for (int nodeIndex = 0; nodeIndex < 4; nodeIndex++)
            {
                Node node = element.Nodes[nodeIndex];
                // Get the gradient of the shape function for this node (constant for linear elements)
                double[] shapeFunctionConstants = element.ShapeFunctions[nodeIndex];
                double shapeFunctionAtPoint = shapeFunctionConstants[0]
                    + shapeFunctionConstants[1]*point.X
                    +shapeFunctionConstants[2] * point.Y
                    +shapeFunctionConstants[3] * point.Z;

                // Voltage at the current node
                double voltageAtNode = _MapNodeIdentifierToResultValue[node.Identifier];

                // Accumulate the electric field: E = - sum(grad(N_i) * voltage_i)
                electricField[0] -= shapeFunctionAtPoint*shapeFunctionConstants[1] * voltageAtNode; // E_x component
                electricField[1] -= shapeFunctionAtPoint * shapeFunctionConstants[2] * voltageAtNode; // E_y component
                electricField[2] -= shapeFunctionAtPoint * shapeFunctionConstants[3] * voltageAtNode; // E_z component
            }
            double conductivity = ((StaticCurrentVolume)element.VolumeIsAPartOf!).Conductivity;
            // Compute the current density: J = sigma * E
            currentDensity[0] = conductivity * electricField[0]; // J_x component
            currentDensity[1] = conductivity * electricField[1]; // J_y component
            currentDensity[2] = conductivity * electricField[2]; // J_z component

            return currentDensity;
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
                double x = nodalCurrentDensities[i++];
                double y = nodalCurrentDensities[i++];
                double z = nodalCurrentDensities[i++];
                double magnitude = GetCurrentDensityMagnitude(x, y, z);
                sum += magnitude;
            }
            double nCurrentDensities = nodalCurrentDensities.Length / 3d;
            return sum / nCurrentDensities;
        }
        private double GetCurrentDensityMagnitude(double x, double y, double z) { 
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) - Math.Pow(z, 2));
        }
        private double[] GetNodalVolumeCurrentDensities() {

            TetrahedralMesh toMesh = _ResultMesh;
            double[] values = new double[toMesh.Nodes.Length * 3];
            int valuesIndex = 0;
            foreach (Node node in toMesh.Nodes)
            {
                Vector3D currentDensity = GetVolumeCurrentDensityForNodeByAveragingElements(node.Identifier);
                values[valuesIndex++] = currentDensity.X;
                values[valuesIndex++] = currentDensity.Y;
                values[valuesIndex++] = currentDensity.Z;
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
        private class NodalContributions {
            public int NContributions;
            public double[] Sum;
            public NodalContributions(double[] first)
            {
                NContributions = 1;
                Sum = new double[] { first[0], first[1], first[2] };
            }
        }
        /*working but not idea. averages from elements*/
        public void ApplyVolumeCurrentDensities(
        TetrahedralMesh meshBeingAppliedTo,
        FieldDOFInfo fieldDOFInfo,
        IBigMatrix K,
        double[] rhs,
        string operationIdentifier,
        CompositeProgressHandler? parentProgressHandler)
        {
            double averageVolume = _ResultMesh.Elements.Select(e => e.ElementVolume).Sum() / (double)_ResultMesh.Elements.Length;

            double averageR0 = 0;
            foreach (Node thisNode in _ResultMesh.Nodes)
            {
                Vector3D total = new Vector3D(0, 0, 0);
                var elementsContainingNode =
                    _ResultMesh.MapNodeToElementsBelongsTo[thisNode.Identifier];
                Vector3D totalForElements = Vector3D.Zeros();
                foreach (var element in elementsContainingNode)
                {
                    Node[] nodes = element.Nodes;
                    double conductivity = ((StaticCurrentVolume)element.VolumeIsAPartOf!).Conductivity;





                    // Retrieve voltages at each node
                    double[] voltages = new double[] {
                    _MapNodeIdentifierToResultValue[element.NodeA.Identifier],
                    _MapNodeIdentifierToResultValue[element.NodeB.Identifier],
                    _MapNodeIdentifierToResultValue[element.NodeC.Identifier],
                    _MapNodeIdentifierToResultValue[element.NodeD.Identifier]
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
                        totalForElements += currentDensityContribution.Scale(element.ElementVolume);
                    }
                }
                total += totalForElements.Scale(1d / (double)elementsContainingNode.Count());
                int globalIndex = meshBeingAppliedTo.MapNodeIdentifierToGlobalIndex[thisNode.Identifier];
                averageR0 += total.X;
                rhs[(globalIndex *3)] = total.X;
                rhs[(globalIndex * 3)+1] = total.Y;
                rhs[(globalIndex * 3)+2] = total.Z;
            }
            averageR0 = averageR0 / ((double)_ResultMesh.Nodes.Length);
        }
        public double[] ComputeInflowOutflowForElementWithShapeFunctions(TetrahedronElement element, double conductivity)
        {
            // Element volume
            double elementVolume = element.ElementVolume;

            // Nodal voltages (computed from static current conduction analysis)
            double[] voltages = new double[] {
            _MapNodeIdentifierToResultValue[element.NodeA.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeB.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeC.Identifier],
            _MapNodeIdentifierToResultValue[element.NodeD.Identifier]
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
                    double xSign = Math.Sign(element.Nodes[j].X - thisNode.X);
                    double ySign = Math.Sign(element.Nodes[j].Y - thisNode.Y);
                    double zSign = Math.Sign(element.Nodes[j].Z - thisNode.Z);

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
        }
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
        private Vector3D GetVolumeCurrentDensityForElement(TetrahedronElement element)
        {
            double[] V = new double[] {
                _MapNodeIdentifierToResultValue[element.NodeA.Identifier],
                _MapNodeIdentifierToResultValue[element.NodeB.Identifier],
                _MapNodeIdentifierToResultValue[element.NodeC.Identifier],
                _MapNodeIdentifierToResultValue[element.NodeD.Identifier]
            };
            double[] J = VectorHelper.Scale(
                MatrixHelper.MatrixMultiplyByVector(element.ScalarBMatrix, V),
                -1 * ((StaticCurrentVolume)element.VolumeIsAPartOf!).Conductivity);
            return new Vector3D(J[0], J[1], J[2]);
        }/**/



        //CF working
        private Vector3D GetVolumeCurrentDensityForNodeByAveragingElements(int nodeIdentifier)
        {
            Vector3D totalWeightedCurrentDensity = new Vector3D(0, 0, 0);
            if (!_ResultMesh.MapNodeToElementsBelongsTo.TryGetValue(nodeIdentifier, out List<TetrahedronElement>? elementsContainingNode))
            {
                return totalWeightedCurrentDensity;
            }
            double totalVolume = 0;
            foreach (var element in elementsContainingNode)
            {
                double elementVolume = element.ElementVolume;
                Vector3D elementCurrentDensityWeighted = GetVolumeCurrentDensityForElement(element).Scale(elementVolume);
                totalVolume += elementVolume;
                totalWeightedCurrentDensity += elementCurrentDensityWeighted;
            }
            Vector3D averageCurrentDensity = totalWeightedCurrentDensity / totalVolume;

            return averageCurrentDensity;
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