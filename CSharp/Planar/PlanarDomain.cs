
using Core.Geometry;
using Core.Graphics;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.MtlFiles;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
namespace FiniteElementAnalysis.Polyhedrals
{

    public class PlanarDomain
    {
        private HashSet<PlanarNode> _Nodes;
        public HashSet<PlanarNode> Nodes { get { return _Nodes; } }
        private HashSet<PlanarEdge> _Edges = new HashSet<PlanarEdge>();
        public HashSet<PlanarEdge> BoundaryEdges { get { return _Edges; } }
        public PlanarSegment[] Segments { get; }
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes { get; }
        public PlanarDomain(BoundariesCollection boundaries, VolumesCollection volumes,
            PlanarNode[] nodes, PlanarSegment[] segments, PlanarEdge[] edges)
        {
            Boundaries = boundaries;
            Volumes = volumes;
            _Nodes = nodes.ToHashSet();
            _Edges = edges.ToHashSet();
            Segments = segments;
            ValidateDomainComplete();
        }
        private void ValidateDomainComplete() { 
            //TODO be done later i know it works
        }
        public void CheckForNodesTooCloseTogether(double distance = 0.0001)
        {
            PlanarNode[] nodes = Nodes.ToArray();
            for(int nodeIndex=0; nodeIndex<Nodes.Count; nodeIndex++)
            {
                PlanarNode node = nodes[nodeIndex];
                for (int otherNodeIndex = nodeIndex + 1; otherNodeIndex < Nodes.Count; otherNodeIndex++) {

                    PlanarNode otherNode = nodes[otherNodeIndex];
                    double magnitude = (node - otherNode).Magnitude();
                    if (magnitude < distance) { 
                        
                    }
                }
            }
        }
        private Rectangle? _Rectangle;
        public Rectangle Rectangle
        {
            get
            {
                if (_Rectangle != null) return _Rectangle;
                PlanarNode[] nodes = Nodes.ToArray();
                double xMin;
                double xMax;
                double yMin;
                double yMax;
                xMin = nodes[0].X;
                xMax = xMin;
                yMin = nodes[0].Y;
                yMax = yMin;
                for (int i = 1; i < nodes.Length; i++)
                {
                    var node = nodes[i];
                    double x = node.X;
                    double y = node.Y;
                    if (xMin > x)
                    {
                        xMin = x;
                    }
                    if (xMax < x)
                    {
                        xMax = x;
                    }
                    if (yMin > y)
                    {
                        yMin = y;
                    }
                    if (yMax < y)
                    {
                        yMax = y;
                    }
                }
                _Rectangle =  new Rectangle(xFrom:xMin, yFrom:yMin, xTo:xMax, yTo:yMax);
                return _Rectangle;
            }
        }
        public void ApplyColours(MtlFile file) {

            var mapMaterialNameToRGBF = file.Materials.ToDictionary(m => m.Name, m => m.Kd);
            foreach (var volume in Volumes.Entries) {

                if (mapMaterialNameToRGBF.TryGetValue(volume.Name, out RGBF? color) && color != null)
                {
                    volume.Color = color;
                }
            }
            foreach (var boundary in Boundaries.Entries)
            {

                if (mapMaterialNameToRGBF.TryGetValue(boundary.Name, out RGBF? color) && color != null)
                {
                    boundary.Color = color;
                }
            }
        }
    }
}