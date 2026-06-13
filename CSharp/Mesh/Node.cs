using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
namespace FiniteElementAnalysis.Mesh
{

    public class Node : Vector3D
    {
        public int Identifier { get; }
        private double[]? _Attributes;
        public double[] Attributes { get { return _Attributes!; } }
        public Boundary? Boundary { get; }

        public override bool Equals(object? obj)
        {
            return obj is Node node &&
                   Identifier == node.Identifier;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier);
        }
        public double[]? Values { get; set; }
        public double ScalarValue
        {
            get
            {
                if (Values == null) throw new Exception($"{nameof(Values)}  was not set");
                if (Values.Length > 1) throw new Exception($"{nameof(Values)} did not contain a single value for a single degree of freedom");
                return Values[0];
            }
        }
        public object Element { get; set; }
        public Node(int identifier, double x, double y, double z, double[]? attributes, Boundary? outerBoundary)
            : base(x, y, z)
        {
            Identifier = identifier;
            _Attributes = attributes;
            Boundary = outerBoundary;
        }
    }
}