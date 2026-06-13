using FiniteElementAnalysis.Boundaries.Electrostatic;

namespace FiniteElementAnalysis.Boundaries
{
    public abstract class LinearBoundaryWithAdditionalRowsColumns : LinearBoundary, ISystemMatrixModifyingBoundary
    {
        public override bool IsNonLinear => false;

        public int NAdditionalRowsColumnsRequired { get; }
        private int[]? _IndicesAssigned;
        public int[]? IndicesAssigned { get { 
                return _IndicesAssigned;
            } set
            {
                if (_IndicesAssigned != null) {
                    throw new Exception("Already set");
                }
                _IndicesAssigned = value;
            } }

        public bool IndicesHaveBeenAssigned
        {
            get { 
                return IndicesAssigned!=null&&IndicesAssigned.Length>0;
            }
        }

        protected LinearBoundaryWithAdditionalRowsColumns(BoundaryConditionType type, string name,
            int nAdditionalRowsColumns) : base(type, name)
        {
            NAdditionalRowsColumnsRequired = nAdditionalRowsColumns;
        }
        protected LinearBoundaryWithAdditionalRowsColumns(BoundaryConditionType type, string name,
            bool twoElementsAllowed, int nAdditionalRowsColumns) : base(type, name, twoElementsAllowed)
        {
            NAdditionalRowsColumnsRequired = nAdditionalRowsColumns;
        }
    }
}