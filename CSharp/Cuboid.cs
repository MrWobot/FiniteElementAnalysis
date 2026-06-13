using FiniteElementAnalysis.Polyhedrals;
namespace FiniteElementAnalysis
{

    public class Cuboid : Polyhedral
    {
        public PolyhedralNode A { get; }
        public PolyhedralNode B { get; }
        public PolyhedralNode C { get; }
        public PolyhedralNode D { get; }
        public PolyhedralNode E { get; }
        public PolyhedralNode F { get; }
        public PolyhedralNode G { get; }
        public PolyhedralNode H { get; }
        public PolyhedralFacet Left { get; }
        public PolyhedralFacet Right { get;} 
        public PolyhedralFacet Top { get; }
        public PolyhedralFacet Bottom { get; }
        public PolyhedralFacet Front { get; }
        public PolyhedralFacet Back { get; }
        public Cuboid(
            PolyhedralNode a, PolyhedralNode b, PolyhedralNode c,
            PolyhedralNode d, PolyhedralNode e, PolyhedralNode f,
            PolyhedralNode g, PolyhedralNode h, PolyhedralFacet left,
            PolyhedralFacet right, PolyhedralFacet top, PolyhedralFacet bottom,
            PolyhedralFacet front, PolyhedralFacet back) 
            :base (a, b, c, d, e, f, g, h){
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
            G = g; 
            H = h;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Front = front;
            Back = back;
        }
    }
}