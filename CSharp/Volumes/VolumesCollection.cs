
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
namespace FiniteElementAnalysis.Boundaries
{
    public class VolumesCollection
    {
        private List<Volume> _Entries = new List<Volume>();
        private Dictionary<string, Volume> _MapNameToBoundary = new Dictionary<string, Volume>();
        public Volume[] Entries { get { return _Entries.ToArray(); } }
        public bool HasEntries { get { return _Entries.Count > 0; } }
        public bool HasMultipleOperationEntries { 
            get {
                return _Entries.Where(e => typeof(MultipleOperationVolume).IsAssignableFrom(e.GetType())).Any(); 
            } 
        }
        public VolumesCollection(params Volume[] volumes) {
            foreach (Volume volume in volumes)
                Add(volume);
        }
        public void Add(Volume volume)
        {
            if (_Entries.Contains(volume)) return;
            if (_MapNameToBoundary.ContainsKey(volume.Name)) throw new ArgumentException($"Already has a volume named \"{volume.Name}\"");
            _Entries.Add(volume);
            _MapNameToBoundary[volume.Name] = volume;
        }
        public Volume? TryGetVolumeByName(string name) {
            _MapNameToBoundary.TryGetValue(name, out Volume? volume);
            return volume;
        }
      /*  public Volume[] MatchVolumesByGroupNames(string[] groupNames) {
            return groupNames.SelectMany(g => _Entries.Where(e => e.GroupIdentifierRegex.Match(g).Success)).ToArray();
        }*/
    }
}