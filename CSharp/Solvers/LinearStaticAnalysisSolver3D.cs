using Core.Exceptions;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries.Statics;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.Solvers
{
    public class LinearStaticAnalysisSolver3D:LinearStaticAnalysisSolverBase
    {
        protected LinearStaticAnalysisSolver3D() : base(new FieldDOFInfo(3, 6, FieldOperationType.StrainDisplacement))
        {
        }
        protected override void ApplySurfaceTractionNeumannBoundary(
            SurfaceTractionNeumannBoundaryBase boundaryBase, IMesh iMesh, double[] rhs)
        {
            throw new NeverUsedOrDebuggedException();
            SurfaceTractionNeumannBoundary3D boundary = (SurfaceTractionNeumannBoundary3D)boundaryBase;
            TetrahedralMesh mesh = (TetrahedralMesh)iMesh;
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null || faces.Length == 0)
                throw new InvalidOperationException("No faces found for boundary.");

            foreach (BoundaryFace face in faces)
            {
                // Traction is force per unit area — each face contributes independently
                // No totalArea fraction needed unlike SurfaceForceNeumannBoundary
                Vector3D forcePerNode = boundary.Tractions * face.Area / face.Nodes.Length;

                foreach (Node node in face.Nodes)
                {
                    int globalNodeIndex = mesh.MapNodeIndexToGlobalIndex[node.Index];
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 0] += forcePerNode.X;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 1] += forcePerNode.Y;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 2] += forcePerNode.Z;
                }
            }
        }
        protected override void ApplySurfaceForceNeumannBoundary(
            SurfaceForceNeumannBoundaryBase boundary, IMesh iMesh, double[] rhs)
        {
            throw new NeverUsedOrDebuggedException();
            TetrahedralMesh mesh = (TetrahedralMesh)iMesh;
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null || faces.Length == 0)
                throw new InvalidOperationException("No faces found for boundary.");

            // Compute total surface area
            double totalArea = faces.Sum(face => face.Area);
            if (totalArea <= 0)
                throw new InvalidOperationException("Total surface area must be greater than zero.");

            SurfaceForceNeumannBoundary3D boundary3D = (SurfaceForceNeumannBoundary3D)boundary;

            foreach (BoundaryFace face in faces)
            {
                double areaFraction = face.Area / totalArea;
                Vector3D forcePerFace = boundary3D.Forces * areaFraction;
                Vector3D forcePerNode = forcePerFace / face.Nodes.Length;

                foreach (Node node in face.Nodes)
                {
                    int globalNodeIndex = mesh.MapNodeIndexToGlobalIndex[node.Index];
                    // Translational DOFs only — standard linear tetrahedral has 3 DOF per node
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 0] += forcePerNode.X;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 1] += forcePerNode.Y;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 2] += forcePerNode.Z;
                }
            }
        }
    }
}