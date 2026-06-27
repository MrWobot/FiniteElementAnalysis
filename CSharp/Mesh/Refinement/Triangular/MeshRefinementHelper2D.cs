using Core.Collections;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Planar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;

namespace FiniteElementAnalysis.Mesh.Refinement.Triangular
{
    public class MeshRefinementHelper2D
    {
        public static PlanarDomain Refine(PlanarDomain domain, double globalMaximumArea)
        {

            var polygon = new Polygon();
            var getVertexFromNode = Create_GetVertexFromNode(domain, polygon);
            var mapMarkerToBoundary = new Dictionary<int, Boundary>();
            Func<Boundary, int> getBoundaryMarker = Create_GetBoundaryMarker(mapMarkerToBoundary);
            foreach (var boundaryEdge in domain.BoundaryEdges)
            {
                var vertex1 = getVertexFromNode(boundaryEdge.Node1);
                var vertex2 = getVertexFromNode(boundaryEdge.Node2);
                polygon.Add(new Segment(vertex1, vertex2, getBoundaryMarker(boundaryEdge.Boundary)));
            }
            var mapMarkerToVolume = new Dictionary<int, Volume>();
            Func<Volume, int> getVolumeMarker = Create_GetVolumeMarker(mapMarkerToVolume);
            foreach (var segment in domain.Segments)
            {
                var (centroidX, centroidY) = segment.Centroid;
                polygon.Regions.Add(new RegionPointer(centroidX, centroidY, getVolumeMarker(segment.VolumeBelongsTo), segment.VolumeBelongsTo.MaximumVolumeConstraint));
            }

            var quality = new QualityOptions()
            {
                MinimumAngle = 20,
                MaximumArea = globalMaximumArea // controls global mesh density
            };
            var triangulator = new GenericMesher();
            var mesh2D = (TriangleNet.Mesh)triangulator.Triangulate(polygon, quality);
            return ToNewPlanarDomain(mesh2D, mapMarkerToBoundary, mapMarkerToVolume, domain.Volumes, domain.Boundaries);
        }
        private static PlanarDomain ToNewPlanarDomain(TriangleNet.Mesh mesh,
            Dictionary<int, Boundary> mapMarkerToBoundary,
            Dictionary<int, Volume> mapMarkerToVolume,
            VolumesCollection volumes,
            BoundariesCollection boundaries)
        {
            var mapVertexToPlanarNode = new Dictionary<Vertex, PlanarNode>();
            var newPlanarSegments = new List<PlanarSegment>();
            var newPlanarBoundaryEdges = new List<PlanarEdge>();
            int nextNodeIndex = 0;
            foreach (var vertex in mesh.Vertices)
            {
                mapVertexToPlanarNode[vertex] = new PlanarNode(vertex.X, vertex.Y, nextNodeIndex++);
            }
            Create_MapNodePairsToSegment_GetSegmentFromNodePair(
            out Action<PlanarSegment> mapNodePairsToSegment,
            out Func<PlanarNode, PlanarNode, PlanarSegment[]> getSegmentsFromNodePair);
            foreach (var triangle in mesh.Triangles)
            {
                if (!mapMarkerToVolume.TryGetValue(triangle.Label, out Volume? volume) || volume == null)
                {
                    throw new Exception("Should never happen");
                }
                var newPlanarSegment = new PlanarSegment(
                    new PlanarNode[] {
                        mapVertexToPlanarNode[triangle.GetVertex(0)],
                        mapVertexToPlanarNode[triangle.GetVertex(1)],
                        mapVertexToPlanarNode[triangle.GetVertex(2)]
                    },
                    volume
                );
                mapNodePairsToSegment(newPlanarSegment);
                newPlanarSegments.Add(newPlanarSegment);

            }
            foreach (var segment in mesh.Segments)
            {
                if (!mapMarkerToBoundary.TryGetValue(segment.Label, out Boundary? boundary) || boundary == null)
                {
                    throw new Exception("Should never happen");
                }
                var node1 = mapVertexToPlanarNode[segment.GetVertex(0)];
                var node2 = mapVertexToPlanarNode[segment.GetVertex(1)];

                newPlanarBoundaryEdges.Add(new PlanarEdge(
                    node1,
                    node2,
                    boundary,
                    getSegmentsFromNodePair(node1, node2)
                    )
                );
            }
            return new PlanarDomain(boundaries, volumes, mapVertexToPlanarNode.Values.ToArray(), newPlanarSegments.ToArray(), newPlanarBoundaryEdges.ToArray());
        }
        private static void Create_MapNodePairsToSegment_GetSegmentFromNodePair(
            out Action<PlanarSegment> mapNodePairsToSegment,
            out Func<PlanarNode, PlanarNode, PlanarSegment[]> getSegmentsFromNodePair
        )
        {
            var mapNodePairIndicesAscendingToPlanarSegment = new DictionaryDictionary<int, int, List<PlanarSegment>>();
            var _map = (int nodeIndexLower, int nodeIndexUpper, PlanarSegment newPlanarSegment) =>
            {
                if (mapNodePairIndicesAscendingToPlanarSegment.TryGetValue(nodeIndexLower, nodeIndexUpper, out List<PlanarSegment> planarSegments))
                {
                    planarSegments!.Add(newPlanarSegment);
                    return;
                }
                mapNodePairIndicesAscendingToPlanarSegment.Map(nodeIndexLower, nodeIndexUpper, new List<PlanarSegment> { newPlanarSegment });
            };
            mapNodePairsToSegment = (planarSegment) =>
            {
                var nodesOrderedById = planarSegment.Nodes
                    .OrderBy(n => n.Index).Select(n => n.Index).ToArray();
                _map(nodesOrderedById[0], nodesOrderedById[1], planarSegment);
                _map(nodesOrderedById[0], nodesOrderedById[2], planarSegment);
                _map(nodesOrderedById[1], nodesOrderedById[2], planarSegment);
            };
            getSegmentsFromNodePair = (planarNodeA, planarNodeB) =>
            {
                if (planarNodeA.Index < planarNodeB.Index)
                {
                    mapNodePairIndicesAscendingToPlanarSegment.TryGetValue(planarNodeA.Index, planarNodeB.Index, out List<PlanarSegment>? segments);
                    return segments!.ToArray();
                }
                else
                {
                    mapNodePairIndicesAscendingToPlanarSegment.TryGetValue(planarNodeB.Index, planarNodeA.Index, out List<PlanarSegment>? segments);
                    return segments!.ToArray();
                }
            };
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
        private static Func<Volume, int> Create_GetVolumeMarker(Dictionary<int, Volume> mapMarkerToVolume)
        {
            int nextMarker = 1;
            var mapVolumeToMarker = new Dictionary<Volume, int>();
            return (boundary) =>
            {
                if (mapVolumeToMarker.TryGetValue(boundary, out int marker))
                {
                    return marker;
                }
                marker = nextMarker++;
                mapMarkerToVolume[marker] = boundary;
                mapVolumeToMarker[boundary] = marker;
                return marker;
            };
        }
        private static Func<PlanarNode, Vertex> Create_GetVertexFromNode(PlanarDomain domain, Polygon polygon)
        {
            var mapNodeToVertex = new Dictionary<PlanarNode, Vertex>();
            foreach (var node in domain.Nodes)
            {
                if (mapNodeToVertex.ContainsKey(node)) continue;
                var vertex = new Vertex(node.X, node.Y);
                mapNodeToVertex.Add(node, vertex);
                polygon.Add(vertex);
            }
            return (node) =>
            {
                return mapNodeToVertex[node];
            };
        }
    }
}
