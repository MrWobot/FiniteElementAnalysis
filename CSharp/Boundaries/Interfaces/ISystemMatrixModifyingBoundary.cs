namespace FiniteElementAnalysis.Boundaries.Electrostatic
{
    public interface ISystemMatrixModifyingBoundary
    {
        public int NAdditionalRowsColumnsRequired { get; }
        public int[] IndicesAssigned { get; set; }
        public bool IndicesHaveBeenAssigned { get; }
    }
}