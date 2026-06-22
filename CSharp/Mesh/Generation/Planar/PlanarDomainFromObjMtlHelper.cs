using Core.Enums;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Polyhedrals;
using System.Text;
using FiniteElementAnalysis.Enums;
using Core.Collections;
using System.Net;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.MtlFiles;
using Core.Graphics;
namespace FiniteElementAnalysis.Mesh.Generation.Planar
{

    public static class PlanarDomainFromObjMtlHelper
    {


        // -------------------------------------------------------------------------
        // Public entry point
        // -------------------------------------------------------------------------

        public static PlanarDomain Read(
            byte[] objFileBytes,
            VolumesCollection volumes,
            BoundariesCollection boundaries,
            out Dictionary<int, Boundary> mapMarkerToBoundary,
            Units units,
            double maxDistanceNodeMergeMeters,
            NormalAxis normalAxis = NormalAxis.AutoDetermine)
        {
            string str = Encoding.ASCII.GetString(objFileBytes);
            string[] lines = str.Split('\n');

            // Step 1: Parse raw OBJ into vertices and faces
            List<RawVertex> rawVertices = new List<RawVertex>();
            List<RawFace> rawFaces = new List<RawFace>();
            Step1_ParseRawObj(lines, units, rawVertices, rawFaces);

            // Step 2: Resolve the normal axis (AutoDetermine picks skinniest dimension)
            NormalAxis resolvedAxis = Step2_ResolveNormalAxis(normalAxis, rawVertices);

            // Step 3: Classify each face as parallel-to-normal-axis (planar) or perpendicular (boundary)
            List<ClassifiedFace> classifiedFaces = Step3_ClassifyFaces(rawFaces, resolvedAxis);

            DelegateGetAxisCoordinate getAxisCoordinate = Create_GetAxisCoordinate(resolvedAxis);
            // Step 4: From planar faces, find the near plane (closest coordinate to 0 along normal axis)
            double nearPlaneCoordinate = Step4_FindNearPlaneCoordinate(classifiedFaces, getAxisCoordinate, maxDistanceNodeMergeMeters);

            // Step 5: Separate planar faces into volume faces (on near plane) and ignored faces (far plane)
            List<RawFace> volumeFaces = Step5_ExtractVolumeFaces(classifiedFaces, getAxisCoordinate, nearPlaneCoordinate, maxDistanceNodeMergeMeters);

            // Step 6: Extract boundary faces (perpendicular to normal axis)
            List<RawFace> boundaryFaces = Step6_ExtractBoundaryFaces(classifiedFaces);

            DelegateProjectVertex projectVertex = Create_ProjectVertex(resolvedAxis);
            var getReusedPlanarNode = Create_GetReusedPlanarNode(projectVertex, maxDistanceNodeMergeMeters, out Func<PlanarNode[]> getAllNodes);
            Create_AddGetNodeToSegmentMappings(out Func<PlanarSegment, bool> addNodeToSegmentMappings,
                out Func<PlanarNode, PlanarNode, PlanarSegment[]?> getPlanarSegmentsEdgeBelongsTo, out Func<PlanarSegment[]> getAllSegments);

            var mapVolumeNameToVolume = volumes.Entries.ToDictionary(b => b.Name, b => b);
            foreach (RawFace volumeFace in volumeFaces)
            {
                PlanarNode[] planarNodesAscendingIndex = volumeFace.Vertices
                    .Select(v => getReusedPlanarNode(v, true)).OrderBy(n => n.Index).ToArray();
                if (string.IsNullOrEmpty(volumeFace.MaterialName))
                    continue;
                if (!mapVolumeNameToVolume.TryGetValue(volumeFace.MaterialName, out Volume? volume))
                {
                    continue;
                }
                var planarSegment = new PlanarSegment(planarNodesAscendingIndex, volume);
                if (addNodeToSegmentMappings(planarSegment))
                {
                    foreach (var n in planarNodesAscendingIndex)
                        n.AddBelongsTo(planarSegment);
                }
            }
            var mapBoundaryNameToBoundary = boundaries.Entries.ToDictionary(b => b.Name, b => b);
            mapMarkerToBoundary = new Dictionary<int, Boundary>();
            var getBoundaryMarker = Create_GetBoundaryMarker(mapMarkerToBoundary);
            var planarEdges = new List<PlanarEdge>();
            foreach (RawFace boundaryFace in boundaryFaces)
            {

                var verticesOnPlaneWithAxis = boundaryFace.Vertices
                    .Select(v => new VertexAndAxisCoordinate(v, Math.Abs(getAxisCoordinate(v))))
                    .OrderBy(o => o.AxisCoordinate)
                    .Where(o => Math.Abs(nearPlaneCoordinate - o.AxisCoordinate) <= maxDistanceNodeMergeMeters)
                    .Take(2)
                    .ToArray();
                if (verticesOnPlaneWithAxis.Count() < 2)
                {
                    continue;
                }
                var edgePlanarNodes = verticesOnPlaneWithAxis.Select(o => getReusedPlanarNode(o.Vertex, false)).ToArray();
                if (edgePlanarNodes.Length != 2) throw new Exception("Something went very wrong");
                PlanarSegment[]? segmentsBelongsTo = getPlanarSegmentsEdgeBelongsTo(edgePlanarNodes[0], edgePlanarNodes[1]);
                if (segmentsBelongsTo == null || segmentsBelongsTo.Length > 2)
                {
                    throw new Exception("Something went very wrong");
                }
                if (string.IsNullOrEmpty(boundaryFace.MaterialName))
                    continue;
                if (!mapBoundaryNameToBoundary.TryGetValue(boundaryFace.MaterialName, out Boundary? boundary))
                {
                    continue;
                }
                planarEdges.Add(new PlanarEdge(edgePlanarNodes[0], edgePlanarNodes[1], getBoundaryMarker(boundary), boundary, segmentsBelongsTo!));
            }
            PlanarDomain domain = new PlanarDomain(boundaries, volumes, getAllNodes(), getAllSegments(), planarEdges.ToArray());
            return domain;
        }
        private static Func<Boundary, int> Create_GetBoundaryMarker(Dictionary<int, Boundary> mapMarkerToBoundary)
        {
            int nextMarker = 1;
            var mapBoundaryToMarker = new Dictionary<Boundary, int>();
            return (boundary) =>
            {
                if (mapBoundaryToMarker.TryGetValue(boundary, out int marker))
                {
                    return marker;
                }
                marker = nextMarker++;
                mapMarkerToBoundary[marker] = boundary;
                mapBoundaryToMarker[boundary] = marker;
                return marker;
            };
        }
        private static void Create_AddGetNodeToSegmentMappings(out Func<PlanarSegment, bool> add,
                out Func<PlanarNode, PlanarNode, PlanarSegment[]?> getPlanarSegmentsEdgeBelongsTo,
                out Func<PlanarSegment[]> getAll)
        {

            var mapNodePairsAscendingIndexToSegments = new DictionaryDictionary<int, int, List<PlanarSegment>>();
            var seenSegments = new DictionaryDictionaryDictionary<int, PlanarSegment>();
            var _add = (int nodeAIndex, int nodeBIndex, PlanarSegment planarSegment) =>
            {

                if (mapNodePairsAscendingIndexToSegments.TryGetValue(
                    nodeAIndex, nodeBIndex, out List<PlanarSegment>? planarSegments))
                {
                    planarSegments!.Add(planarSegment);
                    return;
                }
                mapNodePairsAscendingIndexToSegments.Map(
                    nodeAIndex, nodeBIndex, new List<PlanarSegment> { planarSegment });
            };
            add = (planarSegment) =>
            {
                var nodeIndices = planarSegment.Nodes.Select(n => n.Index).OrderBy(i => i).ToArray();
                if (seenSegments.ContainsKey(nodeIndices[0], nodeIndices[1], nodeIndices[2]))
                {
                    return false;
                }
                seenSegments.Map(nodeIndices[0], nodeIndices[1], nodeIndices[2], planarSegment);
                var nodes = planarSegment.Nodes;
                _add(nodes[0].Index, nodes[1].Index, planarSegment);
                _add(nodes[0].Index, nodes[2].Index, planarSegment);
                _add(nodes[1].Index, nodes[2].Index, planarSegment);
                return true;
            };
            getPlanarSegmentsEdgeBelongsTo = (a, b) =>
            {
                int indexSmallest = a.Index;
                int indexLargest = b.Index;
                if (a.Index > b.Index)
                {
                    indexSmallest = b.Index;
                    indexLargest = a.Index;
                }
                mapNodePairsAscendingIndexToSegments.TryGetValue(indexSmallest, indexLargest, out List<PlanarSegment> segments);
                return segments?.ToArray();
            };
            getAll = () => seenSegments.GetValues().ToArray();
        }
        private static Func<RawVertex, bool, PlanarNode> Create_GetReusedPlanarNode(DelegateProjectVertex projectVertex,
            double tolerance,
            out Func<PlanarNode[]> getAllNodes)
        {
            var pool = new List<PlanarNode>();
            int nextNodeIndex = 0;
            getAllNodes = () => pool.ToArray();
            return (RawVertex rawVertex, bool canCreateNew) =>
            {

                (double a, double b) = projectVertex(rawVertex);
                PlanarNode? planarNode = pool.FirstOrDefault(n =>
                    Math.Sqrt(Math.Pow(n.X - a, 2) + Math.Pow(n.Y - b, 2)) < tolerance);
                if (planarNode != null)
                {
                    return planarNode!;
                }
                if (!canCreateNew)
                {
                    throw new Exception("Should not be creating a new node here");
                }
                planarNode = new PlanarNode(a, b);
                planarNode.Index = nextNodeIndex++;
                pool.Add(planarNode);
                return planarNode;
            };
        }
        // -------------------------------------------------------------------------
        // Step 1: Parse raw OBJ — vertices and faces with material names
        // -------------------------------------------------------------------------

        private static void Step1_ParseRawObj(
            string[] lines,
            Units units,
            List<RawVertex> rawVertices,
            List<RawFace> rawFaces)
        {
            Func<double, double> scaleToMeters = units switch
            {
                Units.Meters => u => u,
                Units.Millimeters => u => 0.001 * u,
                Units.Micrometers => u => 0.000001 * u,
                _ => u => u
            };

            string? currentMaterial = null;
            // OBJ vertex list — 1-indexed in face definitions
            List<RawVertex> vertexList = new List<RawVertex>();

            foreach (string rawLine in lines)
            {
                string line = rawLine.Replace("\r", "").Trim();
                if (line.Length < 1) continue;
                if (line[0] == '#') continue;

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1) continue;

                switch (parts[0])
                {
                    case "usemtl":
                        currentMaterial = parts.Length > 1 ? parts[1] : null;
                        break;

                    case "v":
                        if (parts.Length < 4) continue;
                        double x = scaleToMeters(double.Parse(parts[1]));
                        double y = scaleToMeters(double.Parse(parts[2]));
                        double z = scaleToMeters(double.Parse(parts[3]));
                        vertexList.Add(new RawVertex(x, y, z));
                        break;

                    case "f":
                        RawVertex[] faceVerts = ParseFaceVertices(parts, vertexList);
                        if (faceVerts.Length >= 3)
                        {
                            rawFaces.Add(new RawFace(faceVerts, currentMaterial));
                        }
                        break;
                }
            }

            rawVertices.AddRange(vertexList);
        }

        private static RawVertex[] ParseFaceVertices(string[] parts, List<RawVertex> vertexList)
        {
            // OBJ face entries can be "v", "v/vt", "v/vt/vn", "v//vn" — extract vertex index only
            List<RawVertex> result = new List<RawVertex>();
            for (int i = 1; i < parts.Length; i++)
            {
                string entry = parts[i];
                if (string.IsNullOrWhiteSpace(entry)) continue;
                string indexStr = entry.Contains('/') ? entry.Substring(0, entry.IndexOf('/')) : entry;
                if (int.TryParse(indexStr, out int index))
                {
                    int zeroBasedIndex = index - 1;
                    if (zeroBasedIndex >= 0 && zeroBasedIndex < vertexList.Count)
                        result.Add(vertexList[zeroBasedIndex]);
                }
            }
            return result.ToArray();
        }

        // -------------------------------------------------------------------------
        // Step 2: Resolve normal axis — AutoDetermine picks skinniest dimension
        // -------------------------------------------------------------------------

        private static NormalAxis Step2_ResolveNormalAxis(NormalAxis requested, List<RawVertex> vertices)
        {
            if (requested != NormalAxis.AutoDetermine)
                return requested;

            if (vertices.Count == 0)
                throw new Exception("No vertices found in OBJ file");

            double minX = vertices.Min(v => v.X), maxX = vertices.Max(v => v.X);
            double minY = vertices.Min(v => v.Y), maxY = vertices.Max(v => v.Y);
            double minZ = vertices.Min(v => v.Z), maxZ = vertices.Max(v => v.Z);

            double rangeX = maxX - minX;
            double rangeY = maxY - minY;
            double rangeZ = maxZ - minZ;

            if (rangeX <= rangeY && rangeX <= rangeZ) return NormalAxis.X;
            if (rangeY <= rangeX && rangeY <= rangeZ) return NormalAxis.Y;
            return NormalAxis.Z;
        }

        // -------------------------------------------------------------------------
        // Step 3: Classify faces by their normal relative to the resolved normal axis
        // -------------------------------------------------------------------------

        private static List<ClassifiedFace> Step3_ClassifyFaces(
            List<RawFace> rawFaces,
            NormalAxis resolvedAxis)
        {
            List<ClassifiedFace> result = new List<ClassifiedFace>();

            foreach (RawFace face in rawFaces)
            {
                if (face.Vertices.Length < 3) continue;

                Vector3D normal = ComputeFaceNormal(face);
                bool isParallel = IsFaceNormalParallelToAxis(normal, resolvedAxis);
                result.Add(new ClassifiedFace(face, normal, isParallel));
            }

            return result;
        }

        private static Vector3D ComputeFaceNormal(RawFace face)
        {
            // Use first three vertices to compute normal via cross product
            RawVertex v0 = face.Vertices[0];
            RawVertex v1 = face.Vertices[1];
            RawVertex v2 = face.Vertices[2];

            Vector3D a = new Vector3D(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
            Vector3D b = new Vector3D(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);

            return a.Cross(b);
        }

        private static bool IsFaceNormalParallelToAxis(Vector3D normal, NormalAxis axis)
        {
            // A face is parallel to the normal axis if its normal points predominantly along that axis
            // i.e. the component along the axis is the largest component
            double absX = Math.Abs(normal.X);
            double absY = Math.Abs(normal.Y);
            double absZ = Math.Abs(normal.Z);

            double axisComponent = axis switch
            {
                NormalAxis.X => absX,
                NormalAxis.Y => absY,
                NormalAxis.Z => absZ,
                _ => throw new Exception("Unresolved axis")
            };

            double maxComponent = Math.Max(absX, Math.Max(absY, absZ));

            // Parallel if the axis component is dominant
            return axisComponent == maxComponent;
        }

        // -------------------------------------------------------------------------
        // Step 4: Find near plane coordinate (planar faces only, closest to 0)
        // -------------------------------------------------------------------------

        private static double Step4_FindNearPlaneCoordinate(
            List<ClassifiedFace> classifiedFaces,
            DelegateGetAxisCoordinate getAxisCoordinate,
            double tolerance)
        {
            // Get all distinct coordinate values along the normal axis from planar faces
            List<double> planeCoordinates = new List<double>();

            foreach (ClassifiedFace face in classifiedFaces.Where(f => f.IsParallelToNormalAxis))
            {
                foreach (RawVertex vertex in face.RawFace.Vertices)
                {
                    double coord = getAxisCoordinate(vertex);
                    // Group into distinct planes using tolerance
                    bool alreadySeen = planeCoordinates.Any(c => Math.Abs(c - coord) < tolerance);
                    if (!alreadySeen)
                        planeCoordinates.Add(coord);
                }
            }
            /*
            if (planeCoordinates.Count > 1)
                throw new Exception($"Expected only one planes along the normal axis but found {planeCoordinates.Count}. You should delete the back planes after to then colour the boundaries. Check the model has been extruded correctly.");
            */
            // Near plane = the one closest to 0
            return planeCoordinates.OrderBy(c => Math.Abs(c)).First();
        }

        // -------------------------------------------------------------------------
        // Step 5: Extract volume faces — planar faces on the near plane with a material
        // -------------------------------------------------------------------------

        private static List<RawFace> Step5_ExtractVolumeFaces(
            List<ClassifiedFace> classifiedFaces,
            DelegateGetAxisCoordinate getAxisCoordinate,
            double nearPlaneCoordinate,
            double tolerance)
        {
            List<RawFace> result = new List<RawFace>();

            foreach (ClassifiedFace face in classifiedFaces.Where(f => f.IsParallelToNormalAxis))
            {
                // Check if all vertices of this face are on the near plane
                bool onNearPlane = face.RawFace.Vertices.All(v =>
                    Math.Abs(getAxisCoordinate(v) - nearPlaneCoordinate) < tolerance);

                if (!onNearPlane) continue;
                if (face.RawFace.MaterialName == null) continue;

                result.Add(face.RawFace);
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Step 6: Extract boundary faces — perpendicular to normal axis
        // -------------------------------------------------------------------------

        private static List<RawFace> Step6_ExtractBoundaryFaces(List<ClassifiedFace> classifiedFaces)
        {
            return classifiedFaces
                .Where(f => !f.IsParallelToNormalAxis && f.RawFace.MaterialName != null)
                .Select(f => f.RawFace)
                .ToList();
        }
        private static DelegateGetAxisCoordinate Create_GetAxisCoordinate(NormalAxis axis) => axis switch
        {
            NormalAxis.X => (vertex) => vertex.X,
            NormalAxis.Y => (vertex) => vertex.Y,
            NormalAxis.Z => (vertex) => vertex.Z,
            NormalAxis.AutoDetermine => throw new Exception($"Should not be {nameof(NormalAxis.AutoDetermine)} here because auto determination should yield a direction"),
            _ => throw new Exception("Unresolved axis")
        };

        private static DelegateProjectVertex Create_ProjectVertex(NormalAxis axis) => axis switch
        {
            NormalAxis.X => (vertex) => (vertex.Y, vertex.Z),
            NormalAxis.Y => (vertex) => (vertex.X, vertex.Z),
            NormalAxis.Z => (vertex) => (vertex.X, vertex.Y),
            NormalAxis.AutoDetermine => throw new Exception($"Should not be {nameof(NormalAxis.AutoDetermine)} here because auto determination should yield a direction"),
            _ => throw new Exception("Unresolved axis")
        };
    }
}
