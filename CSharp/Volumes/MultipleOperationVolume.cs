namespace FiniteElementAnalysis.Boundaries
{
    public class MultipleOperationVolume : Volume
    {
        private Dictionary<string, Volume?>
            _MapOperationIdentifierToVolume = new Dictionary<string, Volume?>();
        public MultipleOperationVolume(
            string name,
            string operationIdentifierA,
            Volume volumeA,
            double maximumTetrahedralVolumeConstraint = -1)
            :base(name, maximumTetrahedralVolumeConstraint)
        {
            Add(volumeA, operationIdentifierA);
        }
        public MultipleOperationVolume(
            string name,
            string operationIdentifierA, Volume volumeA,
            string operationIdentifierB, Volume volumeB,
            double maximumTetrahedralVolumeConstraint = -1)
            : base(name, maximumTetrahedralVolumeConstraint)
        {
            Add(volumeA, operationIdentifierA);
            Add(volumeB, operationIdentifierB);
        }
        public MultipleOperationVolume(
            string name,
            string operationIdentifierA, Volume volumeA,
            string operationIdentifierB, Volume volumeB,
            string operationIdentifierC, Volume volumeC,
            double maximumTetrahedralVolumeConstraint = -1)
            : base(name, maximumTetrahedralVolumeConstraint)
        {
            Add(volumeA, operationIdentifierA);
            Add(volumeB, operationIdentifierB);
            Add(volumeC, operationIdentifierC);
        }

        public void Add(Volume? volume, string operationIdentifier)
        {
            if (volume!=null&&_MapOperationIdentifierToVolume.Any())
            {
                if (volume.Name != Name)
                {
                    throw new ArgumentException($"All volumes supplied to a {nameof(MultipleOperationVolume)} must share the same name as the {nameof(MultipleOperationVolume)} supplied name parameter");
                }
            }
            if (_MapOperationIdentifierToVolume.ContainsKey(operationIdentifier))
            {
                throw new ArgumentException($"A {nameof(Volume)} is already mapped for the {nameof(operationIdentifier)} \"{operationIdentifier}\"");
            }
            _MapOperationIdentifierToVolume[operationIdentifier] = volume;
        }
        public Volume GetByOperationIdentifierNoNull(string operationIdentifier)
        {
            if (_MapOperationIdentifierToVolume.TryGetValue(operationIdentifier, out Volume? volume))
            {
                if (volume != null)
                {
                    return volume;
                }
                throw new Exception($"No {nameof(Volume)} for the {nameof(operationIdentifier)} \"{operationIdentifier}\" was mapped as null.");
            }
            throw new Exception($"No {nameof(Volume)} for the {nameof(operationIdentifier)} \"{operationIdentifier}\". You probably included elements in the sub-mesh which are not used for this operation.");
        }
        public Volume? GetByOperationIdentifierAllowNull(string operationIdentifier)
        {
            _MapOperationIdentifierToVolume.TryGetValue(operationIdentifier, out Volume? volume);
            return volume;
        }
    }
}