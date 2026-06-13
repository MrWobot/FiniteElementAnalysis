using Core.Enums;
using Core.Geometry;
using Core.Maths.Tensors;
using Core.Trees;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Polyhedrals;
using ScottPlot.Plottables;
using System.Text;

namespace FiniteElementAnalysis.Mesh.Generation
{
    public static class ObjFileToPoly
    {
        //https://en.wikipedia.org/wiki/Wavefront_.obj_file
        public static PolyhedralDomain Read(byte[] bytes, VolumesCollection volumes, BoundariesCollection boundaries,
            out Dictionary<int, Boundary> mapMarkerToBoundary, Units units, double maxDistanceNodeMergeMeters)
        {
            string str = Encoding.ASCII.GetString(bytes);
            string[] lines = str.Split("\n");
            return ReadLines(lines, boundaries, volumes, out mapMarkerToBoundary, units, maxDistanceNodeMergeMeters);
        }
        private static PolyhedralDomain ReadLines(
            IEnumerable<string> lines,
            BoundariesCollection boundaries,
            VolumesCollection volumes,
            out Dictionary<int, Boundary> mapMarkerToBoundary, Units units, double maxDistanceNodeMergeMeters)
        {
            if (!boundaries.HasEntries)
                throw new ArgumentException("No boundaries were provided");
            if (!volumes.HasEntries)
                throw new ArgumentException("No volumes were provided");
            PolyhedralDomain domain = new PolyhedralDomain(boundaries, volumes);
            List<PolyhedralNode> vertices = new List<PolyhedralNode>();
            List<PolyhedralFacet> faces = new List<PolyhedralFacet>();
            Boundary? currentBoundary = null;
            Volume? currentVolume = null;
            Func<int> getCurrentBoundaryMarker =
                Get_GetCurrentBoundaryMarkerCorrespondingToBoundaryVolumePair(
                    () => currentBoundary, out mapMarkerToBoundary);
            Func<string[], PolyhedralFacet?> parseFaceElement = Get_ParseFaceElement(
                domain, vertices, getCurrentBoundaryMarker);
            Action<Volume, string[]> addVolumeNodes = Get_AddVolumeNodes(
                volumes, vertices, out Action doVolumePoints);
            Func<double, double> scaleToMeters = units switch
            {
                Units.Meters => u => u,
                Units.Millimeters => u => 0.001 * u,
                Units.Micrometers => u => 0.000001 * u
            };
            Dictionary<int, Cuboid3D> mapVertexIndexToOverlappingNodeCuboid
                = new Dictionary<int, Cuboid3D>();
            Func<PolyhedralNode, Cuboid3D> getBoundingCuboidOverlappingVertices = (node) =>
            {
                var cuboid = Cuboid3D.ConstructFromCenterAndHalfSize(node, maxDistanceNodeMergeMeters);
                mapVertexIndexToOverlappingNodeCuboid[node.Index] = cuboid;
                return cuboid;
            };
            BVH<PolyhedralNode> bvhVertexProximity = new BVH<PolyhedralNode>(
                null,
                getBoundingCuboidOverlappingVertices,
                isPointInsideEntry: (vertex, point) =>
                    mapVertexIndexToOverlappingNodeCuboid[vertex.Index].Contains(point)
            );
            int nLine = 0;
            HashSet<Boundary> seenBoundaries = new HashSet<Boundary>();
            foreach (string line in lines)
            {
                nLine++;
                if (line.Length < 1) continue;
                if (line[0] == '#') continue;
                string[] entries = line.Replace("\r", "").Split(' ');
                if (entries.Length < 1) continue;
                string prefix = entries[0];
                switch (prefix)
                {
                    case "usemtl":
                        string boundaryName = entries[1];
                        currentBoundary = boundaries.TryGetBoundaryByName(boundaryName);
                        currentVolume = volumes.TryGetVolumeByName(boundaryName);
                        if (currentBoundary != null && currentVolume != null)
                            throw new Exception($"Both a volume and boundary shared the name \"{boundaryName}\"");
                        if (currentBoundary != null)
                        {
                            seenBoundaries.Add(currentBoundary);
                        }
                        //if (currentBoundary == null) throw new Exception($"Had no boundary with name \"{boundaryName}\"");
                        break;
                    case "v":
                        double x = scaleToMeters(double.Parse(entries[1])), y = scaleToMeters(double.Parse(entries[2])), z = scaleToMeters(double.Parse(entries[3]));
                        if (entries.Length != 4) throw new Exception($"Exception on line {nLine}. Wrong number of entries for a vertex");
                        var newVector = new Vector3D(x, y, z);
                        var existingCloseVertices = bvhVertexProximity.QueryBVH(newVector);
                        if (existingCloseVertices.Any())
                        {
                            vertices.Add(existingCloseVertices.OrderBy(v => v.DistanceTo(newVector)).First());
                        }
                        else
                        {
                            var vertex = new PolyhedralNode(x, y, z, domain);
                            vertices.Add(vertex);
                            bvhVertexProximity.Insert(vertex);
                        }
                        break;
                    case "f":
                        if (currentBoundary != null)
                        //This is important because it ignores the double faces where the other side is blank
                        {
                            PolyhedralFacet? face = parseFaceElement(entries);
                            if (face != null)
                            {
                                faces.Add(face);
                            }
                        }
                        else
                        {
                            if (currentVolume != null)
                            {
                                addVolumeNodes(currentVolume, entries);
                            }
                        }
                        break;
                    case "g":

                        /*string[] groupNames = entries.Skip(1).ToArray();
                        Volume[] matchingVolumes = volumes.MatchVolumesByGroupNames(groupNames).ToArray();
                        if (matchingVolumes.Length > 1) {
                            throw new Exception($"Matched multiple volumes {string.Join(',', matchingVolumes.Select(v => v.Name))}");
                        }
                        currentVolume = matchingVolumes.FirstOrDefault();*/
                        break;
                }
            }
            doVolumePoints();
            string[] unseenBoundaryNames = boundaries.Entries.Where(b => !seenBoundaries.Contains(b)).Select(b => b.Name).ToArray();
            if (unseenBoundaryNames.Length > 0)
            {
                throw new Exception($"No nodes were present for the following boundaries: [{string.Join(',', unseenBoundaryNames.Select(b => $"\"{b}\""))}]");
            }
            //var boundaryMarkers = domain.Facets.GroupBy(f => f.BoundaryMarker).Select(g => g.First().BoundaryMarker).ToArray();
            return domain;
        }/*
        private static void RemoveVerticesUnusedByFaces(List<PolyhedralFacet> faces,
            ref List<PolyhedralNode> nodes){
            nodes = faces.SelectMany(face => face.Polygons.SelectMany(p => p.Nodes)).GroupBy(v => v).Select(g => g.First()).ToList();
        }*/
        private static Func<string[], PolyhedralFacet?> Get_ParseFaceElement(
            PolyhedralDomain domain, List<PolyhedralNode> vertices,
            Func<int> getCurrentBoundaryMarker)
        {
            Func<string, int> extractVertexIndex = (entry) => int.Parse(entry.Substring(0, entry.IndexOf('/'))) - 1;
            return (entries) =>
            {//New
                var nodes =
                        entries.Skip(1).Where(e => e != "").Select(entry => vertices[extractVertexIndex(entry)]).ToArray();
                bool isFaceClosedByNodesMerting = nodes.GroupBy(n => n.Index).Where(g => g.Count() > 1).Any();
                if (isFaceClosedByNodesMerting)
                {
                    return null;
                }
                return new PolyhedralFacet(domain, getCurrentBoundaryMarker(),
                    new PolyhedralPolygon(nodes));
            };
        }
        private static Action<Volume, string[]> Get_AddVolumeNodes(
            VolumesCollection volumes, List<PolyhedralNode> vertices, out Action doPointCalculations)
        {
            Dictionary<Volume, List<Vector3D>> mapVolumeToMarkersDiscovered = new Dictionary<Volume, List<Vector3D>>();
            Func<string, int> extractVertexIndex = (entry) => int.Parse(entry.Substring(0, entry.IndexOf('/'))) - 1;
            doPointCalculations = () =>
            {
                int nextRegion = 0;
                foreach (Volume volume in volumes.Entries)
                {
                    if (!mapVolumeToMarkersDiscovered.TryGetValue(volume, out List<Vector3D>? volumeNodes))
                    {
                        throw new Exception($"Discovered no volume marker nodes for volume \"{volume.Name}\"");
                    }
                    volume.VolumeMarkerPoints = volumeNodes.ToArray();
                    volume.Region = nextRegion++;

                }
            };
            return (currentVolume, entries) =>
            {
                if (!mapVolumeToMarkersDiscovered.TryGetValue(currentVolume, out List<Vector3D>? volumeNodes))
                {
                    mapVolumeToMarkersDiscovered[currentVolume] = volumeNodes = new List<Vector3D> { };
                }
                double sumX = 0, sumY = 0, sumZ = 0;
                foreach (PolyhedralNode node in entries.Skip(1).Where(e => e != "").Select(entry => vertices[extractVertexIndex(entry)]))
                {
                    sumX += node.X;
                    sumY += node.Y;
                    sumZ += node.Z;
                }
                mapVolumeToMarkersDiscovered[currentVolume].Add(new Vector3D(sumX / 3d, sumY / 3d, sumZ / 3d));

            };
        }
        private static Func<int> Get_GetCurrentBoundaryMarkerCorrespondingToBoundaryVolumePair(
            Func<Boundary?> getCurrentBoundary, out Dictionary<int, Boundary> mapMarkerToBoundary)
        {
            Dictionary<Boundary, int> mapBoundaryToMarker =
                new Dictionary<Boundary, int>();
            Dictionary<int, Boundary> mapMarkerToBoundaryInternal = new Dictionary<int, Boundary>();
            int nextMarkerValue = 1;
            mapMarkerToBoundary = mapMarkerToBoundaryInternal;
            return () =>
            {
                Boundary? boundary = getCurrentBoundary();
                if (boundary == null)
                    throw new Exception("Face did not have any boundary. Check the obj file");
                int marker;
                if (mapBoundaryToMarker.TryGetValue(boundary, out marker))
                {
                    return marker;
                }
                marker = nextMarkerValue++;
                mapBoundaryToMarker[boundary] = marker;
                mapMarkerToBoundaryInternal[marker] = boundary;
                return marker;
            };
        }
    }
}