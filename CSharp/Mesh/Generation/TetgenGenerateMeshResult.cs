using Core.Arguments;
using Core.Collections;
using Core.FileSystem;
using Core.Maths.Tensors;
using Core.Trees;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Mesh.Generation
{
    [Serializable]
    public class TetgenGenerateMeshResult
    {
        public string DirectoryPath { get; protected set; }
        public int ExitCode { get; protected set; }
        public string NodeFilePath { get { return Path.Combine(DirectoryPath, "mesh.1.node"); } protected set { } }
        public string ElementFilePath { get { return Path.Combine(DirectoryPath, "mesh.1.ele"); } protected set { } }
        public string FaceFilePath { get { return Path.Combine(DirectoryPath, "mesh.1.face"); } protected set { } }
        public string MeshSkippedFaceFilePath { get { return Path.Combine(DirectoryPath, "mesh_skipped.face"); } protected set { } }
        public string MeshSkippedNodeFilePath { get { return Path.Combine(DirectoryPath, "mesh_skipped.node"); } protected set { } }
        public string Output { get; protected set; }
        public string ErrorMessage { get; protected set; }
        public TetgenGenerateMeshResult(int exitCode, string output, string errorMessage, string directoryPath)
        {
            DirectoryPath = directoryPath;
            ExitCode = exitCode;
            Output = output;
            ErrorMessage = errorMessage;
        }
        private TetgenGenerateMeshResult()
        {

        }
        private static readonly Regex _ExtractNodesFromTwoLineSegmentsAreExactlyOverlapping
            = new Regex("\\[([0-9]+),\\s*([0-9]+)\\]");
        private static readonly Regex _ExtractNodesFromTwoFacetsAreOverlapping
            = new Regex(".*\\(([0-9]+),\\s*([0-9]+),\\s*([0-9]+)\\).*");
        public string GetMoreExceptionInfo()
        {
            switch (ExitCode) {
                case 3:
                    string[] lines = Output.Split(Environment.NewLine.ToCharArray()).Where(l=>l.Length>0).ToArray();
                    int lineIndex = 0;
                    StringBuilder sb = new StringBuilder();
                    while (lineIndex < lines.Length) {
                        string line = lines[lineIndex++];
                        if(line.Contains("Two line segments are exactly overlapping"))
                        {
                            Func<string, Tuple<int, int>> extractLineNodes = (str) =>
                            {
                                var regexMatch = _ExtractNodesFromTwoLineSegmentsAreExactlyOverlapping.Match(str);
                                int node1 = int.Parse(regexMatch.Groups[1].Value);
                                int node2 = int.Parse(regexMatch.Groups[2].Value);
                                return new Tuple<int, int>(node1, node2);
                            };
                            string firstLine = lines[lineIndex++];
                            string secondLine = lines[lineIndex++];
                            var firstPair = extractLineNodes(firstLine);
                            var secondPair = extractLineNodes(secondLine);
                            Node[] nodes = ReadNodes(GetBoundaryAlwaysNull);
                            var firstLineA = nodes[firstPair.Item1];
                            var firstLineB = nodes[firstPair.Item2];
                            var secondLineA = nodes[secondPair.Item1];
                            var secondLineB = nodes[secondPair.Item2];

                            sb.AppendLine($"Overlapping line segment: {firstLineA.ToString()} => {firstLineB.ToString()} and {secondLineA.ToString()} => {secondLineB.ToString()}");
                        }
                        else
                        if (line.Contains("Two facets are overlapping"))
                        {
                            Func<string, int[]> extractLineNodes = (str) =>
                            {
                                int openBracketIndex = str.IndexOf("(");
                                int closeBracketIndex = str.IndexOf(")");
                                string commaSeperated = str.Substring(openBracketIndex+1, closeBracketIndex-(openBracketIndex+1));
                                int[] nodes = commaSeperated.Split(",").Select(s => int.Parse(s)).ToArray();
                                return nodes;
                            };
                            var extractedNodes = extractLineNodes(line);
                            Node[] nodes = ReadNodes(GetBoundaryAlwaysNull);
                            sb.AppendLine($"Two facets are overlapping at triangle: [{string.Join(",", extractedNodes.Select(i=>nodes[i].ToString()))}].");
                        }
                        else
                        if (line.Contains("Two segments exactly intersect"))
                        {
                            Func<string, Tuple<int, int>> extractLineNodes = (str) =>
                            {
                                var regexMatch = _ExtractNodesFromTwoLineSegmentsAreExactlyOverlapping.Match(str);
                                int node1 = int.Parse(regexMatch.Groups[1].Value);
                                int node2 = int.Parse(regexMatch.Groups[2].Value);
                                return new Tuple<int, int>(node1, node2);
                            };
                            string firstLine = lines[lineIndex++];
                            string secondLine = lines[lineIndex++];
                            var firstPair = extractLineNodes(firstLine);
                            var secondPair = extractLineNodes(secondLine);
                            Node[] nodes = ReadNodes(GetBoundaryAlwaysNull);
                            var firstLineA = nodes[firstPair.Item1];
                            var firstLineB = nodes[firstPair.Item2];
                            var secondLineA = nodes[secondPair.Item1];
                            var secondLineB = nodes[secondPair.Item2];

                            sb.AppendLine($"Two segments exactly intersect: {firstLineA.ToString()} => {firstLineB.ToString()} and {secondLineA.ToString()} => {secondLineB.ToString()}");
                        }
                    }
                    return sb.ToString();
            }
            return null;// ReadSkippedFaceFile();
        }
        private delegate void DelegateGetBoundaryFromMarker(int marker, out Boundary? boundary,
            out bool wasNotMapped);
        private static void GetBoundaryAlwaysNull(int marker, out Boundary? boundary, out bool wasNotMapped) {
            boundary = null;
            wasNotMapped = false;
        }
        public TetrahedralMesh ToMesh(BoundariesCollection boundaries,
            VolumesCollection volumes,
            Dictionary<int, Boundary> mapMarkerToBoundary)
        {
            DelegateGetBoundaryFromMarker getBoundaryFromMarker = Get_GetBoundaryFromMarker(mapMarkerToBoundary);
            Node[] nodes = ReadNodes(getBoundaryFromMarker);
            Func<int, Node> getNodeFromIndex = (i) => nodes[i];
            /*HandleFaceMapping(
                out Action<Node, TriangleFace> addNodeForFaceMapping,
                out Func<Node[], TriangleFace[]> getFacesForTetrahedron);*/
            TetrahedronElement[] elements = ReadElements(volumes.Entries, getNodeFromIndex);
            DictionaryDictionaryDictionaryList<int, TetrahedronElement> mapThreeNodeIdentifiersToElement
                 = new DictionaryDictionaryDictionaryList<int, TetrahedronElement>();
            foreach (TetrahedronElement element in elements)
            {

                foreach (int[] combination in element.CombinationsOfThreeNodesIdentifiersAscending)
                {
                    mapThreeNodeIdentifiersToElement.Map(combination, element);
                }
            }
            BoundaryFace[] boundaryFaces = ReadBoundaryFaces(getNodeFromIndex, getBoundaryFromMarker,
                mapThreeNodeIdentifiersToElement.QueryNoChecks);
            var bvh = new BVH<TetrahedronElement>(elements.ToList(),
                e => e.BoundingCuboid, (e, p) => e.IsPointInside(p));
            return new TetrahedralMesh(boundaries, volumes, nodes, boundaryFaces, elements, bvh);
        }
        private DelegateGetBoundaryFromMarker Get_GetBoundaryFromMarker(
            Dictionary<int, Boundary> mapMarkerToBoundary) {
            return (int marker, out Boundary? boundary, out bool wasNotMapped) =>
            {
                wasNotMapped = false;
                if (marker < 0)
                {
                    boundary = null;
                    return;
                }
                if (mapMarkerToBoundary.TryGetValue(marker, out boundary))
                {
                    return;
                }
                wasNotMapped = true;
            };
        }
        /// <summary>
        /// Tetgen returns only the boundary faces. So the faces it reads are the boundary faces
        /// </summary>
        /// <param name="getNodeFromIndex"></param>
        /// <param name="getBoundaryFromMarker"></param>
        /// <param name="getElementFromNodeIdentifiers"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private BoundaryFace[] ReadBoundaryFaces(Func<int, Node> getNodeFromIndex,
            DelegateGetBoundaryFromMarker getBoundaryFromMarker, Func<int, int, int, List<TetrahedronElement>> getElementFromNodeIdentifiers)
        {
            string[] faceFileLines = File.ReadAllLines(FaceFilePath);
            string firstLine = faceFileLines[0];
            string[] firstLineEntries = firstLine.Split(' ').Where(e => e != "").ToArray();
            int nFaces = int.Parse(firstLineEntries[0]);
            bool hasBoundaryMarker = int.Parse(firstLineEntries[1]) > 0;
            BoundaryFace[] faces = new BoundaryFace[nFaces];
            HashSet<Node> seenNodes = new HashSet<Node>();
            foreach (string faceFileLine in faceFileLines.Skip(1).Take(nFaces))
            {
                string[] lineEntries = faceFileLine.Split(' ').Where(e => e != "").ToArray();
                int index = int.Parse(lineEntries[0]);
                Node[] nodes = lineEntries
                    .Skip(1)
                    .Take(3)
                    .Select(e => getNodeFromIndex(int.Parse(e)))
                    .ToArray();
                foreach (Node node in nodes)
                    seenNodes.Add(node);
                Boundary? boundary = null;
                if (hasBoundaryMarker)
                {
                    int boundaryMarker = int.Parse(lineEntries[4]);
                    getBoundaryFromMarker(boundaryMarker, out boundary, out bool wasNotMapped);
                    if (wasNotMapped) {
                        throw new Exception($"Boundary with marker {boundaryMarker} was not mapped." +
                            $" It was introduced by Tetgen. Position of nodes:" +
                            $" [{string.Join(",", nodes.Select(n => n.ToString()))}].");
                    }
                }
                var nodesOrdered = nodes.OrderBy(n => n.Identifier).ToArray();
                var elements = getElementFromNodeIdentifiers(nodesOrdered[0].Identifier, nodesOrdered[1].Identifier, nodesOrdered[2].Identifier);
                if (!boundary.MultipleElementsAllowed
                    && !boundary.BoundaryConditionType.Equals(BoundaryConditionType.OperationSpecific)
                    && elements.Count() > 1) throw new Exception($"Multiple elements shared the same boundary face for boundary named \"{boundary.Name}\" with {nameof(BoundaryConditionType)} {Enum.GetName(typeof(BoundaryConditionType), boundary.BoundaryConditionType)}");
                BoundaryFace face = new BoundaryFace(index, nodes, boundary, elements.ToArray());
                faces[index] = face;
            }
            return faces;
        }
        private TetrahedronElement[] ReadElements(Volume[] volumes, Func<int, Node> getNodeFromIndex)
        {
            string[] elementFileLines = File.ReadAllLines(ElementFilePath);
            string firstLine = elementFileLines[0];
            string[] firstLineEntries = firstLine.Split(' ').Where(e => e != "").ToArray();
            int nElements = int.Parse(firstLineEntries[0]);
            int nodesPerTet = int.Parse(firstLineEntries[1]);
            bool hasRegions = int.Parse(firstLineEntries[2]) > 0;
            if (!hasRegions) throw new Exception("Something went wrong. Should always have region attributes to link tetrahedrons to volumes they belong to!");
            if (nodesPerTet != 4)
                throw new NotImplementedException($"Not implemented for {nodesPerTet} node tetrahedrons");
            TetrahedronElement[] elements = new TetrahedronElement[nElements];
            Dictionary<int, Volume> mapRegionToVolume = volumes.ToDictionary(v => v.Region, volumes => volumes);
            foreach (string elementFileLine in elementFileLines.Skip(1).Take(nElements))
            {
                string[] lineEntries = elementFileLine.Split(' ').Where(e => e != "").ToArray();
                int index = int.Parse(lineEntries[0]);
                Node[] nodes = lineEntries
                    .Skip(1)    
                    .Take(4)
                    .Select(e => getNodeFromIndex(int.Parse(e)))
                    .ToArray();
                int region = int.Parse(lineEntries.Last());
                if (!mapRegionToVolume.TryGetValue(region, out Volume? volume))
                {
                    throw new Exception($"Had no volume with region attribute {region} for element {nodes[0].ToString()} {nodes[1].ToString()} {nodes[2].ToString()} {nodes[2].ToString()}");
                }
                TetrahedronElement element = new TetrahedronElement(index, nodes, volume);
                elements[index] = element;
            }
            return elements;
        }
        private Node[] ReadNodes(DelegateGetBoundaryFromMarker getBoundaryFromMarker)
        {
            int currentNodeIdentifier = 0;
            string[] nodeFileLines = File.ReadAllLines(NodeFilePath);
            string firstLine = nodeFileLines[0];
            string[] firstLineEntries = firstLine.Split(' ').Where(e => e != "").ToArray();
            int nNodes = int.Parse(firstLineEntries[0]);
            int nAttributes = int.Parse(firstLineEntries[2]);
            bool hasBoundaryMarker = int.Parse(firstLineEntries[3]) > 0;
            Node[] nodes = new Node[nNodes];
            int xLineIndex = nAttributes + 1;
            int yLineIndex = nAttributes + 2;
            int zLineIndex = nAttributes + 3;
            foreach (string nodeFileLine in nodeFileLines
                .Skip(1).Take(nNodes))
            {
                string[] lineEntries = nodeFileLine.Split(' ').Where(e => e != "").ToArray();
                int index = int.Parse(lineEntries[0]);
                double[]? attributes = null;
                if (nAttributes > 0)
                {
                    attributes = new double[nAttributes];
                    for (int i = 0; i < nAttributes; i++)
                    {
                        attributes[i] = double.Parse(lineEntries[i + 1]);
                    }
                }
                Boundary? boundary = null;
                if (hasBoundaryMarker)
                {
                    int boundaryMarker = int.Parse(lineEntries[4 + nAttributes]);
                    getBoundaryFromMarker(boundaryMarker, out boundary, out bool wasNotMapped);
                    if (wasNotMapped)
                    {
                        throw new Exception($"Boundary with marker {boundaryMarker} was not mapped." +
                            $" It was introduced by Tetgen. Position of nodes:" +
                            $" [{string.Join(",", nodes.Select(n => n.ToString()))}].");
                    }
                }
                double x = double.Parse(lineEntries[xLineIndex]);
                double y = double.Parse(lineEntries[yLineIndex]);
                double z = double.Parse(lineEntries[zLineIndex]);
                Node node = new Node(currentNodeIdentifier++, x, y, z, attributes, boundary);
                nodes[index] = node;
            }
            return nodes;
        }
        private SkippedFaceFile ReadSkippedFaceFile() {
            Node[] nodes = ReadNodes(GetBoundaryAlwaysNull);
            string[] lines = File.ReadAllLines(MeshSkippedFaceFilePath);
            string firstLine = lines[0];
            string[] firstLineSplits = firstLine.Split(' ');
            int nEntries = int.Parse(firstLineSplits[0]);
            int lineIndex = 1;
            Func<string, SkippedFaceFileEntry> parseLine
                = (str) => {
                    string[] splits = str.Split(' ').Where(s=>s.Length>0).ToArray();
                    int index = int.Parse(splits[0]);
                    int nodeIndexA = int.Parse(splits[1]);
                    int nodeIndexB = int.Parse(splits[2]);
                    int nodeIndexC = int.Parse(splits[3]);
                    Node nodeA = nodes[nodeIndexA];
                    Node nodeB = nodes[nodeIndexB];
                    Node nodeC = nodes[nodeIndexC];
                    return new SkippedFaceFileEntry(nodeIndexA, nodeIndexA, nodeIndexB, nodeA, nodeB, nodeC);
                };
            var pairs = new List<SkippedFaceFileEntryPair>();
            while (lineIndex < lines.Length-1)
            {
                string lineA = lines[lineIndex++];
                string lineB = lines[lineIndex++];
                SkippedFaceFileEntry a = parseLine(lineA);
                SkippedFaceFileEntry b = parseLine(lineB);
                var pair = new SkippedFaceFileEntryPair(a, b);
                pairs.Add(pair);
            }
            return new SkippedFaceFile(pairs.ToArray());
        }/*
        private void ApplyVolumesToElements(BVH<TetrahedronElement> elementsBVH, TriangleFace[] faces, TetrahedronElement[] elements, VolumesCollection volumes)
        {
            foreach (Volume volume in volumes.Entries)
            {
                DictionaryDictionaryDictionaryList<int, TetrahedronElement> mapNodesInIndexOrderToElementsTheyFormFacesOn
                    = new DictionaryDictionaryDictionaryList<int, TetrahedronElement>();
                Action<int, int, int, TetrahedronElement> mapFace 
                    = mapNodesInIndexOrderToElementsTheyFormFacesOn.Map;
                Func<int, int, int, List<TetrahedronElement>>? queryNodeMap =
                    mapNodesInIndexOrderToElementsTheyFormFacesOn.QueryNoChecks;
                Func<TetrahedronElement, int[]> getElementIndicesInOrder = (element) 
                    =>element.Nodes.Select(n => n.Index).OrderBy(i => i).ToArray();

                foreach (TetrahedronElement element in elements)
                {
                    var indices = getElementIndicesInOrder(element);
                    mapFace(indices[0], indices[1], indices[2], element);
                    mapFace(indices[0], indices[1], indices[3], element);
                    mapFace(indices[0], indices[2], indices[3], element);
                    mapFace(indices[1], indices[2], indices[3], element);
                }
                DictionaryDictionaryDictionary<Node,TriangleFace> mapNodesInIndexOrderToBoundaryFace =
                    new Core.Collections.DictionaryDictionaryDictionary<Node, TriangleFace>();
                foreach (TriangleFace face in faces) {
                    Node[] faceNodes = face.Nodes.OrderBy(n => n.Index).ToArray();
                    mapNodesInIndexOrderToBoundaryFace.Map(faceNodes[0], faceNodes[1], faceNodes[2], face);
                }
                Func<Node[], bool> isGroupOfNodesBoundary = (nodes) =>
                {
                    if (mapNodesInIndexOrderToBoundaryFace.TryGetValue(nodes[0], nodes[1], nodes[2], out TriangleFace? face)) {
                        if (face.Boundary.BoundaryConditionType.Equals(BoundaryConditionType.OperationSpecific)) { 
                        
                        }
                        return face.Boundary != null;
                    }
                    return false;
                };
                foreach (Vector3D volumeMarkerPoint in volume.VolumeMarkerPoints)
                {
                    HashSet<TetrahedronElement> newElementsWithVolume =
                        elementsBVH.QueryBVH(volumeMarkerPoint).ToHashSet();
                    while (newElementsWithVolume.Count > 0)
                    {
                        foreach (TetrahedronElement e in newElementsWithVolume)//Usually only one but multiple possible
                        {
                            e.VolumeIsAPartOf = volume;
                        }
                        HashSet<TetrahedronElement> nextNewElements = new HashSet<TetrahedronElement>();
                        foreach (TetrahedronElement e in newElementsWithVolume)
                        {
                            Node[] nodesWithoutBoundary = e.Nodes.OrderBy(n => n.Index).ToArray();
                            Node[][] groupsOfNodesToTestIfBoundaryFace = new Node[][] {
                            new Node[] { nodesWithoutBoundary[0], nodesWithoutBoundary[1], nodesWithoutBoundary[2] },
                            new Node[] { nodesWithoutBoundary[0], nodesWithoutBoundary[1], nodesWithoutBoundary[3] },
                            new Node[] { nodesWithoutBoundary[0], nodesWithoutBoundary[2], nodesWithoutBoundary[3] },
                            new Node[] { nodesWithoutBoundary[1], nodesWithoutBoundary[2], nodesWithoutBoundary[3] }
                        };
                            foreach (Node[] groupOfNodesToTest in groupsOfNodesToTestIfBoundaryFace)
                            {
                                if (isGroupOfNodesBoundary(groupOfNodesToTest)) continue;
                                TetrahedronElement? neighbourToSetVolumeOn = queryNodeMap(
                                    groupOfNodesToTest[0].Index,
                                    groupOfNodesToTest[1].Index,
                                    groupOfNodesToTest[2].Index)
                                    ?.Where(element => !element.Equals(e)).FirstOrDefault();
                                if (neighbourToSetVolumeOn != null && !neighbourToSetVolumeOn.HasVolumeIsAPartOf)
                                {
                                    nextNewElements.Add(neighbourToSetVolumeOn);
                                }
                            }
                        }
                        HashSet<TetrahedronElement> oldNewElements = newElementsWithVolume;
                        newElementsWithVolume = nextNewElements;
                        nextNewElements = oldNewElements;
                        nextNewElements.Clear();
                    }
                }
            }
            if (elements.Where(e => !e.HasVolumeIsAPartOf).Any())
                throw new Exception("Not all elements had a volume");
        }*/
    }
}