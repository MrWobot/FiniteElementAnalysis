using FiniteElementAnalysis.Fields;
using MathNet.Numerics;
using ScottPlot.AxisRules;

namespace FiniteElementAnalysis.Boundaries
{
    public class VolumeComponent
    {
        public int NFieldComponents { get; }
        public FieldOperationType FieldOperationType { get; }
        public VolumeComponent(int nFieldComponents, FieldOperationType fieldOperationType) {
            NFieldComponents = nFieldComponents;
            FieldOperationType = fieldOperationType;
        }
    }
}