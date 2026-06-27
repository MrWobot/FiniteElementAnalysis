namespace FiniteElementAnalysis.Mesh.Parsing.Planar.PlanarDomainFromObjMtlHelper_Internal
{
    // A 2D node after projecting out the normal axis
    internal class Node2D
    {
        public double A { get; }  // first of the two remaining axes
        public double B { get; }  // second of the two remaining axes
        public int Index { get; set; }
        public Node2D(double a, double b) { A = a; B = b; }
    }
}
