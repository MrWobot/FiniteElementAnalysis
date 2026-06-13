using Core.Geometry;
using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Integration;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class TetrahedronElement
    {
        public int Identifier { get; }
        public Node[] Nodes { get; }
        public Node NodeA { get { return Nodes[0]; } }
        public Node NodeB { get { return Nodes[1]; } }
        public Node NodeC { get { return Nodes[2]; } }
        public Node NodeD { get { return Nodes[3]; } }
        public Node[] NodesOrderedByIdentifiers
        {
            get
            {
                return Nodes.OrderBy(n => n.Identifier).ToArray();
            }
        }
        public int[] NodeIdentifiersLowToHigh
        {
            get
            {

                return Nodes.Select(n => n.Identifier).OrderBy(i => i).ToArray();
            }
        }
        public double ElementVolume
        {
            get
            {
                return TetrahedronHelper.AbsoluteVolume(NodeA, NodeB, NodeC, NodeD);
            }
        }
        public string? VolumeName { get { return VolumeIsAPartOf?.Name; } }
        public int[][] CombinationsOfThreeNodesIdentifiersAscending
        {
            get
            {
                int[] identifiers = Nodes.OrderBy(n => n.Identifier).Select(n => n.Identifier).ToArray();
                return new int[][] {
                    new int[] { identifiers[0], identifiers[1], identifiers[2] },
                    new int[] { identifiers[0], identifiers[1], identifiers[3]  },
                    new int[] { identifiers[0], identifiers[2], identifiers[3]  },
                    new int[] { identifiers[1], identifiers[2], identifiers[3]  },
                };
            }
        }/*
        public Volume? GetVolumeAllowNull(string operationIdentifier)
        {
            if (_OperationSpecificVolume != null)
            {
                return _OperationSpecificVolume.GetByOperationIdentifierAllowNull(operationIdentifier);
            }
            return _Volume;
        }*/
        public double[] NodalValuesAsVector
        {
            get
            {
                return Nodes.SelectMany(n => n.Values!).ToArray();
            }
        }
        public Volume? VolumeIsAPartOf
        {
            get;
            set;
        }
        public bool HasVolumeIsAPartOf { get { return VolumeIsAPartOf != null; } }
        private double[][] _ShapeFunctions;
        public double[][] ShapeFunctions
        {
            get
            {
                if (_ShapeFunctions == null)
                {
                    _ShapeFunctions = ShapeFunctionHelper.ShapeFunctionTetrahedron(NodeA, NodeB, NodeC, NodeD);
                }
                return _ShapeFunctions;
            }
        }
        public double[][] ShapeFunctionsMatrixN
        {
            get
            {
                // Extract shape function components for each node
                double[] N1 = ShapeFunctions[0];
                double[] N2 = ShapeFunctions[1];
                double[] N3 = ShapeFunctions[2];
                double[] N4 = ShapeFunctions[3];

                // Shape function components
                double b1 = N1[1];
                double b2 = N2[1];
                double b3 = N3[1];
                double b4 = N4[1];

                double c1 = N1[2];
                double c2 = N2[2];
                double c3 = N3[2];
                double c4 = N4[2];

                double d1 = N1[3];
                double d2 = N2[3];
                double d3 = N3[3];
                double d4 = N4[3];

                // Manually constructing N matrix
                double[][] N = new double[][]
                {
                // Row for Ax component (x)
                new double[] { b1, 0,  0,  b2, 0,  0,  b3, 0,  0,  b4, 0,  0  },
        
                // Row for Ay component (y)
                new double[] { 0,  c1, 0,  0,  c2, 0,  0,  c3, 0,  0,  c4, 0  },
        
                // Row for Az component (z)
                new double[] { 0,  0,  d1, 0,  0,  d2, 0,  0,  d3, 0,  0,  d4 }
                };

                return N;
            }
        }

        private double[][] _ScalarBMatrix;
        public double[][] ScalarBMatrix
        {
            get
            {
                if (_ScalarBMatrix == null)
                {
                    _ScalarBMatrix = ShapeFunctionHelper
                        .ComputeScalarBMatrixForTetrahedronElementUsingGradient(ShapeFunctions);
                }
                return _ScalarBMatrix;
            }
        }
        private double[][] _ScalarBMatrixTranspose;
        public double[][] ScalarBMatrixTranspose
        {
            get
            {
                if (_ScalarBMatrixTranspose == null)
                {
                    _ScalarBMatrixTranspose = MatrixHelper.Transpose(ScalarBMatrix);
                }
                return _ScalarBMatrixTranspose;
            }
        }/*
        private double[][] _ScalarInverseBTBMatrix;
        public double[][] ScalarInverseBTBMatrix
        {
            get
            {
                if (_ScalarInverseBTBMatrix == null)
                {
                    _ScalarInverseBTBMatrix = MatrixHelper.MatrixMultiply(ScalarBMatrixTranspose, ScalarBMatrix);
                }
                return _ScalarInverseBTBMatrix;
            }
        }*/
        private double[][] _BMatrix3DOF3FieldComponentsUsingGradients;
        public double[][] BMatrix3DOF3FieldComponentsUsingGradients
        {
            get
            {
                if (_BMatrix3DOF3FieldComponentsUsingGradients == null)
                {
                    _BMatrix3DOF3FieldComponentsUsingGradients = ShapeFunctionHelper
                        .ComputeTetrahedronBMatrix3DOF3FieldComponentsGradients(ShapeFunctions);
                }
                return _BMatrix3DOF3FieldComponentsUsingGradients;
            }
        }
        private double[][] _BMatrix3DOF3FieldComponentsUsingGradientsTranspose;
        public double[][] BMatrix3DOF3FieldComponentsUsingGradientsTranspose
        {
            get
            {
                if (_BMatrix3DOF3FieldComponentsUsingGradientsTranspose == null)
                {
                    _BMatrix3DOF3FieldComponentsUsingGradientsTranspose =
                        MatrixHelper.Transpose(BMatrix3DOF3FieldComponentsUsingGradients);
                }
                return _BMatrix3DOF3FieldComponentsUsingGradientsTranspose;
            }
        }
        private double[][] _BMatrix3DOF3FieldComponentsUsingCurl;
        public double[][] BMatrix3DOF3FieldComponentsUsingCurl
        {
            get
            {
                if (_BMatrix3DOF3FieldComponentsUsingCurl == null)
                {
                    // Assuming we have a helper method to compute the curl of the shape functions
                    _BMatrix3DOF3FieldComponentsUsingCurl = ShapeFunctionHelper
                        .ComputeTetrahedronBMatrix3DOF3FieldComponentsCurl(ShapeFunctions);
                }
                return _BMatrix3DOF3FieldComponentsUsingCurl;
            }
        }

        private double[][] _BMatrix3DOF3FieldComponentsUsingCurlTranspose;
        public double[][] BMatrix3DOF3FieldComponentsUsingCurlTranspose
        {
            get
            {
                if (_BMatrix3DOF3FieldComponentsUsingCurlTranspose == null)
                {
                    // Transpose the curl-based B matrix
                    _BMatrix3DOF3FieldComponentsUsingCurlTranspose = MatrixHelper
                        .Transpose(BMatrix3DOF3FieldComponentsUsingCurl);
                }
                return _BMatrix3DOF3FieldComponentsUsingCurlTranspose;
            }
        }


        private double[][] _BMatrix3DOF6FieldComponentsUsingGradients;
        public double[][] BMatrix3DOF6FieldComponentsUsingGradients
        {
            get
            {
                if (_BMatrix3DOF6FieldComponentsUsingGradients == null)
                {
                    _BMatrix3DOF6FieldComponentsUsingGradients = ShapeFunctionHelper
                        .ComputeTetrahedronBMatrix3DOF6FieldComponentsUsingGradient(ShapeFunctions);
                }
                return _BMatrix3DOF6FieldComponentsUsingGradients;
            }
        }
        private double[][] _BMatrix3DOF6FieldComponentsUsingGradientsTranspose;
        public double[][] BMatrix3DOF6FieldComponentsUsingGradientsTranspose
        {
            get
            {
                if (_BMatrix3DOF6FieldComponentsUsingGradientsTranspose == null)
                {
                    _BMatrix3DOF6FieldComponentsUsingGradientsTranspose = MatrixHelper.Transpose(BMatrix3DOF6FieldComponentsUsingGradients);
                }
                return _BMatrix3DOF6FieldComponentsUsingGradientsTranspose;
            }
        }

        private double[][] _BMatrix3DOF9FieldComponentsUsingGradients;
        public double[][] BMatrix3DOF9FieldComponentsUsingGradients
        {
            get
            {
                if (_BMatrix3DOF9FieldComponentsUsingGradients == null)
                {
                    _BMatrix3DOF9FieldComponentsUsingGradients = ShapeFunctionHelper
                        .ComputeTetrahedronBMatrix3DOF9FieldComponentsUsingGradient(ShapeFunctions);
                }
                return _BMatrix3DOF9FieldComponentsUsingGradients;
            }
        }
        private double[][] _BMatrix3DOF9FieldComponentsUsingGradientsTranspose;
        public double[][] BMatrix3DOF9FieldComponentsUsingGradientsTranspose
        {
            get
            {
                if (_BMatrix3DOF9FieldComponentsUsingGradientsTranspose == null)
                {
                    _BMatrix3DOF9FieldComponentsUsingGradientsTranspose = MatrixHelper.Transpose(BMatrix3DOF9FieldComponentsUsingGradients);
                }
                return _BMatrix3DOF9FieldComponentsUsingGradientsTranspose;
            }
        }

        private double[][] _BMatrix3DOF6FieldComponentsStrainDisplacement;
        public double[][] BMatrix3DOF6FieldComponentsStrainDisplacement
        {
            get
            {
                if (_BMatrix3DOF6FieldComponentsStrainDisplacement == null)
                {
                    _BMatrix3DOF6FieldComponentsStrainDisplacement = ShapeFunctionHelper
                        .ComputeTetrahedronBMatrix3DOF6FieldComponentsStrainDisplacement(ShapeFunctions);
                }
                return _BMatrix3DOF6FieldComponentsStrainDisplacement;
            }
        }
        private double[][] _BMatrix3DOF6FieldComponentsStrainDisplacementTranspose;
        public double[][] BMatrix3DOF6FieldComponentsStrainDisplacementTranspose
        {
            get
            {
                if (_BMatrix3DOF6FieldComponentsStrainDisplacementTranspose == null)
                {
                    _BMatrix3DOF6FieldComponentsStrainDisplacementTranspose = 
                        MatrixHelper.Transpose(BMatrix3DOF6FieldComponentsStrainDisplacement);
                }
                return _BMatrix3DOF6FieldComponentsStrainDisplacementTranspose;
            }
        }
        // Property to hold integration points
        private List<IntegrationPoint> _IntegrationPoints;
        public List<IntegrationPoint> IntegrationPoints
        {
            get
            {
                if (_IntegrationPoints == null)
                {
                    _IntegrationPoints = new List<IntegrationPoint>();

                    // Use the centroid of the tetrahedron as a simple integration point
                    Vector3D centroid = GetCentroid();
                    double weight = ElementVolume; // The weight is the volume of the element
                    _IntegrationPoints.Add(new IntegrationPoint(centroid, weight));
                }
                return _IntegrationPoints;
            }
        }
        private TriangleElementFace[]? _Faces;
        public TriangleElementFace[] Faces { 
            get {
                if (_Faces == null)
                {
                    _Faces = CreateTetrahedronFaces();
                }
                return _Faces;
            } 
        }

        private double? _SignedVolumeMe;
        public TetrahedronElement(int identifier, Node[] nodes, Volume volume) : this(identifier, nodes)
        {
            VolumeIsAPartOf = volume;
        }
        public TetrahedronElement(int identifier, Node[] nodes)
        {
            if (identifier < 0) throw new ArgumentException($"identifier cannot be less than zero. Received value {identifier}");
            if (nodes.Length != 4)
                throw new ArgumentException($"Expected four nodes. Received {nodes.Length}");
            Identifier = identifier;
            Nodes = nodes;
        }
        private Cuboid3D? _BoundingCuboid;
        public Cuboid3D BoundingCuboid
        {
            get
            {
                if (_BoundingCuboid == null)
                {
                    // Initialize min and max values to the first node's position
                    double minX = NodeA.X;
                    double minY = NodeA.Y;
                    double minZ = NodeA.Z;
                    double maxX = NodeA.X;
                    double maxY = NodeA.Y;
                    double maxZ = NodeA.Z;

                    // Check the other nodes to find the minimum and maximum extents
                    foreach (var node in Nodes.Skip(1))
                    {
                        if (node.X < minX) minX = node.X;
                        if (node.Y < minY) minY = node.Y;
                        if (node.Z < minZ) minZ = node.Z;

                        if (node.X > maxX) maxX = node.X;
                        if (node.Y > maxY) maxY = node.Y;
                        if (node.Z > maxZ) maxZ = node.Z;
                    }

                    // Create the bounding cuboid based on these extents
                    Vector3D minPoint = new Vector3D(minX, minY, minZ);
                    Vector3D maxPoint = new Vector3D(maxX, maxY, maxZ);
                    if (minX >= maxX || minY >= maxY || minZ >= maxZ)
                    {
                        throw new Exception("Something went very wrong");
                    }
                    _BoundingCuboid = new Cuboid3D(minPoint, maxPoint);
                }
                return _BoundingCuboid;
            }
        }
        public Vector3D GetCentroid()
        {
            double x = (NodeA.X + NodeB.X + NodeC.X + NodeD.X) / 4.0;
            double y = (NodeA.Y + NodeB.Y + NodeC.Y + NodeD.Y) / 4.0;
            double z = (NodeA.Z + NodeB.Z + NodeC.Z + NodeD.Z) / 4.0;
            return new Vector3D(x, y, z);
        }
        public bool IsPointInside(Vector3D p)
        {
            if (_SignedVolumeMe == null)
            {
                _SignedVolumeMe = TetrahedronHelper.SignedVolume(NodeA, NodeB, NodeC, NodeD);
            }
            double v0P = TetrahedronHelper.SignedVolume(p, NodeB, NodeC, NodeD);
            double v1P = TetrahedronHelper.SignedVolume(NodeA, p, NodeC, NodeD);
            double v2P = TetrahedronHelper.SignedVolume(NodeA, NodeB, p, NodeD);
            double v3P = TetrahedronHelper.SignedVolume(NodeA, NodeB, NodeC, p);
            double epsilon = 1e-10;

            // Compute the sign of the reference volume once
            int referenceSign = Math.Sign(_SignedVolumeMe.Value);

            // Precompute absolute values of the point volumes for comparison with epsilon
            double absV0P = Math.Abs(v0P);
            double absV1P = Math.Abs(v1P);
            double absV2P = Math.Abs(v2P);
            double absV3P = Math.Abs(v3P);

            // Precompute the absolute value of the reference volume for comparison with epsilon
            double absSignedVolumeMe = Math.Abs(_SignedVolumeMe.Value);

            bool sameSign =
                (absV0P < epsilon || absSignedVolumeMe < epsilon || referenceSign == Math.Sign(v0P)) &&
                (absV1P < epsilon || absSignedVolumeMe < epsilon || referenceSign == Math.Sign(v1P)) &&
                (absV2P < epsilon || absSignedVolumeMe < epsilon || referenceSign == Math.Sign(v2P)) &&
                (absV3P < epsilon || absSignedVolumeMe < epsilon || referenceSign == Math.Sign(v3P));
            bool sumEqual = Math.Abs(Math.Abs(v0P) + Math.Abs(v1P) + Math.Abs(v2P) + Math.Abs(v3P) - Math.Abs((double)_SignedVolumeMe)) < 1e-6;
            return sameSign && sumEqual;
        }
        public double[] ComputeShapeFunctionsAtPoint(Vector3D point)
        {
            double[][] shapeFunctions = ShapeFunctions;
            double[] N = new double[4];
            for (int i = 0; i < 4; i++)
            {
                N[i] = shapeFunctions[i][0]
                     + shapeFunctions[i][1] * point.X
                     + shapeFunctions[i][2] * point.Y
                     + shapeFunctions[i][3] * point.Z;
            }

            return N;
        }
        public double InterpolateScalarValueAtPoint(Vector3D point)
        {
            double[] value = InterpolateValueAtPoint(point, 1);
            return value[0];
        }
        public double[] InterpolateValueAtPoint(Vector3D point, int nDegreesFreedom)
        {
            if (!IsPointInside(point)) throw new Exception("Point was not inside");
            double v = 0.0;
            double[] values = new double[nDegreesFreedom];
            double[] shapeFunctionsAtPoint = ComputeShapeFunctionsAtPoint(point);
            for (int dof = 0; dof < nDegreesFreedom; dof++)
            {
                values[dof] = shapeFunctionsAtPoint[0] * Nodes[0].Values![dof]
                + shapeFunctionsAtPoint[1] * Nodes[1].Values![dof]
                + shapeFunctionsAtPoint[2] * Nodes[2].Values![dof]
                + shapeFunctionsAtPoint[3] * Nodes[3].Values![dof];
            }
            return values;
        }
        public double[][] GetBMatrix(FieldDOFInfo fieldDOFInfo)
        {
            return GetBMatrix(fieldDOFInfo.NFieldComponents, 
                fieldDOFInfo.FieldOperationType, fieldDOFInfo.NDegreesOfFreedom);
        }
        public double[][] GetBMatrix(int nFieldComponents, FieldOperationType fieldOperationType, int nDegreesOfFreedom)
        {
            switch (fieldOperationType)
            {
                case FieldOperationType.Gradient:
                    switch (nDegreesOfFreedom)
                    {
                        case 1:
                            if (nFieldComponents == 1)
                            {
                                return ScalarBMatrix;
                            }
                            break;
                        case 3:
                            switch (nFieldComponents)
                            {
                                case 3:
                                    return BMatrix3DOF3FieldComponentsUsingGradients;
                                case 6:
                                    return BMatrix3DOF6FieldComponentsUsingGradients;
                                case 9:
                                    return BMatrix3DOF9FieldComponentsUsingGradients;
                            }
                            break;
                    }
                    break;

                case FieldOperationType.Curl:
                    switch (nDegreesOfFreedom)
                    {
                        case 3:
                            switch (nFieldComponents)
                            {
                                case 3:
                                    return BMatrix3DOF3FieldComponentsUsingCurl;
                            }
                            break;
                    }
                    break;
                case FieldOperationType.StrainDisplacement:
                    switch (nDegreesOfFreedom) {
                        case 3:
                            switch (nFieldComponents)
                            {
                                case 6:
                                    return BMatrix3DOF6FieldComponentsStrainDisplacement;
                            }
                        break;
                    }
                    break;
            }
            throw new NotImplementedException($"{nameof(GetBMatrix)} not implemented for {nameof(FieldOperationType)} {Enum.GetName(typeof(FieldOperationType), fieldOperationType)} with {nDegreesOfFreedom} degrees of freedom and {nFieldComponents} field components");
        }
        public double[][] GetBMatrixTranspose(FieldDOFInfo fieldDOFInfo)
        {
            return GetBMatrixTranspose(fieldDOFInfo.NFieldComponents, fieldDOFInfo.FieldOperationType,
              fieldDOFInfo.NDegreesOfFreedom);
        }
        public double[][] GetBMatrixTranspose(int nFieldComponents, FieldOperationType fieldOperationType, int nDegreesOfFreedom)
        {
            switch (fieldOperationType)
            {
                case FieldOperationType.Gradient:
                    switch (nDegreesOfFreedom)
                    {
                        case 1:
                            if (nFieldComponents == 1)
                            {
                                return ScalarBMatrixTranspose;
                            }
                            break;
                        case 3:
                            switch (nFieldComponents)
                            {
                                case 3:
                                    return BMatrix3DOF3FieldComponentsUsingGradientsTranspose;
                                case 6:
                                    return BMatrix3DOF6FieldComponentsUsingGradientsTranspose;
                                case 9:
                                    return BMatrix3DOF9FieldComponentsUsingGradientsTranspose;
                            }
                            break;
                    }
                    break;

                case FieldOperationType.Curl:
                    switch (nDegreesOfFreedom)
                    {
                        case 3:
                            switch (nFieldComponents)
                            {
                                case 3:
                                    return BMatrix3DOF3FieldComponentsUsingCurlTranspose;
                            }
                            break;
                    }
                    break;
                case FieldOperationType.StrainDisplacement:
                    switch (nDegreesOfFreedom)
                    {
                        case 3:
                            switch (nFieldComponents)
                            {
                                case 6:
                                    return BMatrix3DOF6FieldComponentsStrainDisplacementTranspose;
                            }
                            break;
                    }
                    break;
            }
            throw new NotImplementedException($"{nameof(GetBMatrixTranspose)} not implemented for {nameof(FieldOperationType)} {Enum.GetName(typeof(FieldOperationType), fieldOperationType)} with {nDegreesOfFreedom} degrees of freedom and {nFieldComponents} field components");
        }
        public TriangleElementFace GetFaceOppositeNode(int nodeIdentifier)
        {
            Node[] nodesOfFace = Nodes.Where(n=>n.Identifier!=nodeIdentifier).ToArray();
            if (nodesOfFace.Length != 3)
            {
                throw new Exception("Something went very wrong. Likely node does not belong to element");
            }
            var face = new TriangleElementFace(nodesOfFace[0], nodesOfFace[1], nodesOfFace[2], this);
            Vector3D centroidToFace = face.NodeA - GetCentroid();
            if (face.Normal.Dot(centroidToFace) < 0)
            {
                face.ReverseNodes();
            }
            return face;
        }
        private TriangleElementFace[] CreateTetrahedronFaces()
        {
            List<TriangleElementFace> faces = new List<TriangleElementFace>();

            // Define faces with initial right-hand rule ordering
            faces.Add(new TriangleElementFace(Nodes[0], Nodes[1], Nodes[2], this));
            faces.Add(new TriangleElementFace(Nodes[0], Nodes[1], Nodes[3], this));
            faces.Add(new TriangleElementFace(Nodes[0], Nodes[2], Nodes[3], this));
            faces.Add(new TriangleElementFace(Nodes[1], Nodes[2], Nodes[3], this));

            // Ensure each face normal points outward
            foreach(TriangleElementFace face in faces)
            {
                // Vector from the centroid to one of the face nodes
                Vector3D centroidToFace = face.NodeA - GetCentroid();

                // Check if the normal is pointing inward using the dot product
                if (face.Normal.Dot(centroidToFace) < 0)
                {
                    // Reverse node order to flip normal
                    face.ReverseNodes();
                }
            }
            return faces.ToArray();
        }

        public double GetEffectivePerpendicularArea(Vector3D direction)
        {
            // Normalize the magnetic flux density vector to get its direction
            Vector3D directionUnitVector = direction.Normalize();

            // Create a list to store projected areas of each face along with the face itself
            List<(double projectedArea, TriangleElementFace face)> projectedAreas = new List<(double, TriangleElementFace)>();

            // Iterate over the four faces of the tetrahedron
            foreach (TriangleElementFace face in Faces)
            {
                // Get the normal vector and area of the face
                Vector3D faceNormal = face.Normal;
                double faceArea = face.Area;

                // Calculate the cosine of the angle between B_direction and the face normal
                double cosTheta = Math.Abs(faceNormal.Dot(directionUnitVector) / faceNormal.Magnitude());

                // Calculate the projected area perpendicular to B
                double projectedArea = faceArea * cosTheta;

                // Store the projected area and corresponding face
                projectedAreas.Add((projectedArea, face));
            }

            // Sort faces by projected area in descending order and select the top two
            projectedAreas.Sort((a, b) => b.projectedArea.CompareTo(a.projectedArea));
            double effectivePerpendicularArea = projectedAreas.Take(2).Sum(pa => pa.projectedArea);
            return effectivePerpendicularArea;
        }
        public override bool Equals(object? obj)
        {
            return obj is TetrahedronElement element &&
                   Identifier == element.Identifier;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier);
        }
    }
}