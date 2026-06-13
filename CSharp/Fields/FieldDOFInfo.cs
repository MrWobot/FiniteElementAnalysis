namespace FiniteElementAnalysis.Fields
{
    public struct FieldDOFInfo
    {
        public int NDegreesOfFreedom { get; set; }
        public int NFieldComponents { get; set; }
        public FieldOperationType FieldOperationType { get; set; }

        public FieldDOFInfo(int dof, int components, FieldOperationType fieldOperationType)
        {
            NDegreesOfFreedom = dof;
            NFieldComponents = components;
            FieldOperationType = fieldOperationType;
        }
    }
}