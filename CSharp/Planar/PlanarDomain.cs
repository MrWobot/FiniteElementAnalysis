
using FiniteElementAnalysis.Boundaries;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
namespace FiniteElementAnalysis.Polyhedrals
{

    public class PlanarDomain
    {
        private HashSet<PlanarNode> _Nodes = new HashSet<PlanarNode>();
        public HashSet<PlanarNode> Nodes { get { return _Nodes; } }
        private HashSet<PlanarEdge> _Edges = new HashSet<PlanarEdge>();
        public HashSet<PlanarEdge> Edges { get { return _Edges; } }
        private int _CurrentIndex = 0;
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes{ get; }
        public PlanarDomain(BoundariesCollection boundaries, VolumesCollection volumes)
        {
            Boundaries = boundaries;
            Volumes = volumes;
        }
        public void Add(PlanarNode node)
        {
            if (_Nodes.Add(node))
            {
                node.Index = _CurrentIndex++;
            }
        }
        public void Add(PlanarEdge edge)
        {
            _Edges.Add(edge);
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
    }
}