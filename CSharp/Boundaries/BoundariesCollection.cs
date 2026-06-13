
using FiniteElementAnalysis.Boundaries.Electrostatic;
using MathNet.Numerics.RootFinding;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
namespace FiniteElementAnalysis.Boundaries
{
    public class BoundariesCollection
    {
        private List<Boundary> _Entries = new List<Boundary>();
        private Dictionary<string, Boundary> _MapNameToBoundary = new Dictionary<string, Boundary>();
        public Boundary[] Entries { get { return _Entries.ToArray(); } }
        public bool HasEntries { get { return _Entries.Count > 0; } }
        public bool HasMultipleOperationEntries
        {
            get
            {
                return _Entries.Where(e => typeof(MultipleOperationBoundary).IsAssignableFrom(e.GetType())).Any();
            }
        }
        public Boundary[] NonLinearBoundaries
        {
            get
            {
                return Entries.Where(e => e.IsNonLinear).ToArray();
            }
        }
        private ISystemMatrixModifyingBoundary[] ?_SystemMatrixModifyingBoundaries;
        public ISystemMatrixModifyingBoundary[] SystemMatrixModifyingBoundaries {
            get {
                if (_SystemMatrixModifyingBoundaries==null)
                {
                    _SystemMatrixModifyingBoundaries = Entries.Where(b => typeof(ISystemMatrixModifyingBoundary).IsAssignableFrom(b.GetType()))
                        .Cast<ISystemMatrixModifyingBoundary>()
                        .ToArray();
                }
                return _SystemMatrixModifyingBoundaries;
            }
        }
        public BoundariesCollection(params Boundary?[] boundaries) {
            foreach (Boundary? boundary in boundaries)
            {
                if (boundary == null) continue;
                Add(boundary);
            }
        }
        public Boundary? TryGetBoundaryByName(string name) { 
            _MapNameToBoundary.TryGetValue(name, out Boundary? boundary);
            return boundary;
        }
        public void Add(Boundary boundary) {
            if (_Entries.Contains(boundary)) return;
            if (_MapNameToBoundary.ContainsKey(boundary.Name)) throw new ArgumentException($"Already has a boundary named \"{boundary.Name}\"");
            _Entries.Add(boundary);
            _MapNameToBoundary[boundary.Name]= boundary;
        }
    }
}