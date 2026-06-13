using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Polyhedrals;
using System.Text;

namespace FiniteElementAnalysis.Mesh.Generation
{
    public static class PolyFileGenerator
    {
        // https://wias-berlin.de/software/tetgen/fformats.poly.html
        //https://www.wias-berlin.de/software/tetgen/files/tet.poly
        public static void Generate(string filePath, PolyhedralDomain domain)
        {

            StringBuilder sb = new StringBuilder();

            PolyhedralNode[] nodes = domain.Nodes.ToArray();
            //Nodes
            Nodes(sb, nodes);
            //Facets
            Facets(sb, domain);
            //Holes
            sb.AppendLine($"0");
            sb.AppendLine();
            //Regions
            Regions(sb, domain);
            File.WriteAllText(filePath, sb.ToString());
        }
        private static void Nodes(StringBuilder sb, PolyhedralNode[] nodes)
        {

            sb.AppendLine($"{nodes.Length}  {3}  0  {0/*hasBoundaryInt*/}");
            sb.AppendLine();
            foreach (PolyhedralNode node in nodes)
            {
                sb.AppendLine($"{node.Index}  {node.X}  {node.Y}  {node.Z}");// {node.PrimaryBoundary.Marker}");
            }
            sb.AppendLine();
        }
        private static void Facets(StringBuilder sb, PolyhedralDomain domain)
        {

            PolyhedralFacet[] faces = domain.Facets.ToArray();
            int hasBoundaryInt = domain.Boundaries.HasEntries ? 1 : 0;
            sb.AppendLine($"{domain.Facets.Count}  {hasBoundaryInt}");
            sb.AppendLine();
            foreach (PolyhedralFacet face in faces)
            {
                sb.AppendLine($"{face.Polygons.Count()} {0} {face.BoundaryMarker}");
                foreach (PolyhedralPolygon polygon in face.Polygons)
                {
                    sb.Append(polygon.Nodes.Count());
                    sb.Append(" ");
                    foreach (PolyhedralNode node in polygon.Nodes)
                    {
                        sb.Append(" ");
                        sb.Append(node.Index);
                    }
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
        }
        private static void Regions(StringBuilder sb, PolyhedralDomain domain)
        {
            Volume[] volumes = domain.Volumes.Entries;
            int nRegions = volumes.Select(v => v.VolumeMarkerPoints.Count()).Sum();
            sb.AppendLine(nRegions.ToString());
            int nRegion = 0;
            foreach (Volume volume in volumes)
            {
                foreach (Vector3D volumeMarkerPoint in volume.VolumeMarkerPoints)
                {
                    sb.Append($"{nRegion++} {volumeMarkerPoint.X} {volumeMarkerPoint.Y} {volumeMarkerPoint.Z} {volume.Region} {volume.MaximumTetrahedralVolumeConstraint}");
                    sb.AppendLine();
                }
            }
        }
    }
}