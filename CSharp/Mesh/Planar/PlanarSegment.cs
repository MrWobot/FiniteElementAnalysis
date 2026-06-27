using Core.Graphics;
using FiniteElementAnalysis.Boundaries;
using System.Numerics;

namespace FiniteElementAnalysis.Mesh.Planar
{

    public class PlanarSegment
    {
        public PlanarNode[] Nodes { get; }
        private PlanarEdge _Edge;
        public PlanarEdge Edge
        {

            get
            {
                if (_Edge == null) throw new Exception($"{nameof(Edge)} was not set");
                return _Edge;
            }
        }
        public Volume VolumeBelongsTo { get; }
        public PlanarSegment(PlanarNode[] nodes, Volume volumeBelongsTo)
        {
            if (nodes.Length != 3) throw new Exception("Triangles only");
            Nodes = nodes;
            VolumeBelongsTo = volumeBelongsTo;
            foreach (PlanarNode node in nodes)
            {
                node.AddBelongsTo(this);
            }
        }
        public void SetBelongsTo(PlanarEdge edge)
        {
            _Edge = edge;
        }
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            PlanarSegment? other = obj as PlanarSegment;
            if (other == null) return false;
            if (Nodes.Length != other.Nodes.Length) return false;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].Index == other.Nodes[i].Index)
                    return false;
            }
            return true;
        }
        public (double, double) Centroid
        {
            get
            {
                double xSum = 0;
                double ySum = 0;
                foreach (var node in Nodes)
                {
                    xSum += node.X;
                    ySum += node.Y;
                }
                return (xSum / 3d, ySum / 3d);
            }
        }
    }
}