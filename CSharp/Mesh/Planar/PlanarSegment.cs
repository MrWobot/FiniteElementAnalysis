using Core.Graphics;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Interfaces;
using System.Numerics;

namespace FiniteElementAnalysis.Mesh.Planar
{

    public class PlanarSegment:IElement
    {

        public int Index { get; }

        public INode[] Nodes { get; }
        PlanarNode NodeA { get; }
        PlanarNode NodeB { get; }
        PlanarNode NodeC { get; }
        private double? _Measure;
        public double Measure { get {
                if (_Measure == null)
                {
                    _Measure = Math.Abs((NodeB.X - NodeA.X) * (NodeC.Y - NodeA.Y) - (NodeC.X - NodeA.X) * (NodeB.Y - NodeA.Y)) / 2.0;
                }
                return (double)_Measure;
            } }
        /*
        private PlanarEdge? _Edge;
        public PlanarEdge Edge
        {

            get
            {
                if (_Edge == null) throw new Exception($"{nameof(Edge)} was not set");
                return _Edge;
            }
        }*/
        public Volume VolumeBelongsTo { get; }
        public PlanarSegment(PlanarNode[] nodes, int index, Volume volumeBelongsTo)
        {
            if (nodes.Length != 3) throw new Exception("Triangles only");
            Nodes = nodes;
            NodeA = nodes[0];
            NodeB = nodes[1];
            NodeC = nodes[2];
            Index = index;
            VolumeBelongsTo = volumeBelongsTo;
            foreach (PlanarNode node in nodes)
            {
                node.AddBelongsTo(this);
            }
        }
        /*
        public void SetBelongsTo(PlanarEdge edge)
        {
            _Edge = edge;
        }*/
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

        public double[][] GetBMatrix(FieldDOFInfo fieldDOFInfo)
        {
            throw new NotImplementedException();
        }

        public double[][] GetBMatrix(int nFieldComponents, FieldOperationType fieldOperationType, int nDegreesOfFreedom)
        {
            throw new NotImplementedException();
        }

        public double[][] GetBMatrixTranspose(FieldDOFInfo fieldDOFInfo)
        {
            throw new NotImplementedException();
        }

        public double[][] GetBMatrixTranspose(int nFieldComponents, FieldOperationType fieldOperationType, int nDegreesOfFreedom)
        {
            throw new NotImplementedException();
        }

        public (double, double) Centroid
        {
            get
            {
                double xSum = 0;
                double ySum = 0;
                foreach (INode node in Nodes)
                {
                    PlanarNode planarNode = (PlanarNode)node;
                    xSum += planarNode.X;
                    ySum += planarNode.Y;
                }
                return (xSum / 3d, ySum / 3d);
            }
        }
    }
}