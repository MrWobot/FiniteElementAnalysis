
using FiniteElementAnalysis.Boundaries;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
namespace FiniteElementAnalysis.Polyhedrals
{

    public class PolyhedralDomain
    {
        private HashSet<PolyhedralNode> _Nodes = new HashSet<PolyhedralNode>();
        public HashSet<PolyhedralNode> Nodes { get { return _Nodes; } }
        private HashSet<PolyhedralFacet> _Faces = new HashSet<PolyhedralFacet>();
        public HashSet<PolyhedralFacet> Facets { get { return _Faces; } }
        private int _CurrentIndex = 0;
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes{ get; }
        public PolyhedralDomain(BoundariesCollection boundaries, VolumesCollection volumes)
        {
            Boundaries = boundaries;
            Volumes = volumes;
        }
        public void Add(PolyhedralNode node)
        {
            if (_Nodes.Add(node))
            {
                node.Index = _CurrentIndex++;
            }
        }
        public void Add(PolyhedralFacet face)
        {
            _Faces.Add(face);
        }
        public void CheckForNodesTooCloseTogether(double distance = 0.0001)
        {
            PolyhedralNode[] nodes = Nodes.ToArray();
            for(int nodeIndex=0; nodeIndex<Nodes.Count; nodeIndex++)
            {
                PolyhedralNode node = nodes[nodeIndex];
                for (int otherNodeIndex = nodeIndex + 1; otherNodeIndex < Nodes.Count; otherNodeIndex++) {

                    PolyhedralNode otherNode = nodes[otherNodeIndex];
                    double magnitude = (node - otherNode).Magnitude();
                    if (magnitude < distance) { 
                        
                    }
                }
            }
        }
    }
}