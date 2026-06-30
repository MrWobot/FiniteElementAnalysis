using Core.Geometry;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Mesh.Planar
{

    public class PlanarSegment : IElement
    {
        public int Identifier { get; }

        public IReadOnlyList<INode> Nodes { get; }
        PlanarNode NodeA { get; }
        PlanarNode NodeB { get; }
        PlanarNode NodeC { get; }
        private double? _Measure;
        public double Measure
        {
            get
            {
                if (_Measure == null)
                {
                    _Measure = Math.Abs((NodeB.X - NodeA.X) * (NodeC.Y - NodeA.Y) - (NodeC.X - NodeA.X) * (NodeB.Y - NodeA.Y)) / 2.0;
                }
                return (double)_Measure;
            }
        }
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
        public PlanarSegment(PlanarNode[] planarNodes, int index, Volume volumeBelongsTo)
        {
            if (planarNodes.Length != 3) throw new Exception("Triangles only");
            Nodes = planarNodes;
            NodeA = planarNodes[0];
            NodeB = planarNodes[1];
            NodeC = planarNodes[2];
            Identifier = index;
            VolumeBelongsTo = volumeBelongsTo;
            foreach (PlanarNode node in planarNodes)
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
            if (Nodes.Count != other.Nodes.Count) return false;
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Identifier == other.Nodes[i].Identifier)
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
        public bool IsPointInside(double[] point)
        {
            double[] N = ComputeShapeFunctionsAtPoint(point);
            double epsilon = 1e-10;
            return N[0] >= -epsilon && N[1] >= -epsilon && N[2] >= -epsilon;
        }
        public Rectangle BoundingRectangle
        {
            get
            {
                return new Rectangle(
                    Math.Min(NodeA.X, Math.Min(NodeB.X, NodeC.X)),
                    Math.Min(NodeA.Y, Math.Min(NodeB.Y, NodeC.Y)),
                    Math.Max(NodeA.X, Math.Max(NodeB.X, NodeC.X)),
                    Math.Max(NodeA.Y, Math.Max(NodeB.Y, NodeC.Y))
                );
            }
        }
        private double[] ComputeShapeFunctionsAtPoint(double[] point)
        {
            double totalArea = Measure;
            double x = point[0], y = point[1];
            double xA = NodeA.X, yA = NodeA.Y;
            double xB = NodeB.X, yB = NodeB.Y;
            double xC = NodeC.X, yC = NodeC.Y;
            // Barycentric coordinates
            double NA = ((yB - yC) * (x - xC) + (xC - xB) * (y - yC)) / (2.0 * totalArea);
            double NB = ((yC - yA) * (x - xC) + (xA - xC) * (y - yC)) / (2.0 * totalArea);
            double NC = 1.0 - NA - NB;
            return [NA, NB, NC];
        }

        public double[] InterpolateValueAtPoint(double[] point, int nDegreesFreedom)
        {
            if (!IsPointInside(point)) throw new Exception("Point was not inside");
            double[] N = ComputeShapeFunctionsAtPoint(point);
            double[] values = new double[nDegreesFreedom];
            for (int dof = 0; dof < nDegreesFreedom; dof++)
            {
                values[dof] = N[0] * Nodes[0].Values![dof]
                            + N[1] * Nodes[1].Values![dof]
                            + N[2] * Nodes[2].Values![dof];
            }
            return values;
        }

        public double[] Centroid
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
                return [xSum / 3d, ySum / 3d];
            }
        }
    }
}