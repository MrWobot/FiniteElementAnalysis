namespace FiniteElementAnalysis.Boundaries
{
    public class MultipleOperationBoundary : Boundary
    {
        private Dictionary<string, Boundary>
            _MapOperationIdentifierToBoundary = new Dictionary<string, Boundary>();
        public MultipleOperationBoundary(
            string name,
            string operationIdentifierA,
            Boundary boundaryA)
            :base(BoundaryConditionType.OperationSpecific, name)
        {
            Add(boundaryA, operationIdentifierA);
        }
        public MultipleOperationBoundary(
            string name,
            string operationIdentifierA, Boundary boundaryA,
            string operationIdentifierB, Boundary boundaryB)
            : base(BoundaryConditionType.OperationSpecific, name)
        {
            Add(boundaryA, operationIdentifierA);
            Add(boundaryB, operationIdentifierB);
        }
        public MultipleOperationBoundary(
            string name,
            string operationIdentifierA, Boundary boundaryA,
            string operationIdentifierB, Boundary boundaryB,
            string operationIdentifierC, Boundary boundaryC)
            : base(BoundaryConditionType.OperationSpecific, name)
        {
            Add(boundaryA, operationIdentifierA);
            Add(boundaryB, operationIdentifierB);
            Add(boundaryC, operationIdentifierC);
        }
        public override bool IsNonLinear => throw new NotImplementedException();

        public void Add(Boundary boundary, string operationIdentifier)
        {
            if (_MapOperationIdentifierToBoundary.Any())
            {
                if (boundary!=null&&boundary.Name != Name)
                {
                    throw new ArgumentException($"All boundaries supplied to a {nameof(MultipleOperationBoundary)} must share the same name");
                }
            }
            if (_MapOperationIdentifierToBoundary.ContainsKey(operationIdentifier))
            {
                throw new ArgumentException($"A {nameof(Boundary)} is already mapped for the {nameof(operationIdentifier)} \"{operationIdentifier}\"");
            }
            _MapOperationIdentifierToBoundary[operationIdentifier] = boundary;
        }
        public Boundary GetByOperationIdentifier(string operationIdentifier) {
            if (_MapOperationIdentifierToBoundary.TryGetValue(operationIdentifier, out Boundary? boundary))
                return boundary;
            throw new Exception($"No {nameof(Boundary)} for the {nameof(operationIdentifier)} \"{operationIdentifier}\". You probably included elements in the sub-mesh which are not used for this operation.");

        }
    }
}