using Core.Geometry;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
namespace FiniteElementAnalysis.Polyhedrals
{

    public class PolyhedralNode : Vector3D
    {
        private List<PolyhedralPolygon>? _PolygonsBelongsTo = null;
        public List<PolyhedralPolygon>? PolygonsBelongsTo { get { return _PolygonsBelongsTo; } }
        public int Index { get; set; }
        public PolyhedralNode(double x, double y, double z, PolyhedralDomain domain)
            : base(x, y, z)
        {
            domain.Add(this);
        }
        public void AddBelongsTo(PolyhedralPolygon polygons)
        {
            if (_PolygonsBelongsTo == null)
                _PolygonsBelongsTo = new List<PolyhedralPolygon>();
            _PolygonsBelongsTo.Add(polygons);
        }
    }
}