using RedStarLibrary.GameTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData
{
    // TODO: use this in other places such as LayerConfig
    public class PlacementList : IEnumerable<PlacementInfo>
    {
        public string Name { get; set; } = string.Empty;

        private List<PlacementInfo> _placementList = new();
        public PlacementList() { }
        public PlacementList(string name) { Name = name; }

        public void Add(PlacementInfo placement) { _placementList.Add(placement); }
        public void Remove(PlacementInfo placement) { _placementList.Remove(placement); }

        public PlacementInfo this[int index]
        {
            get => _placementList[index];
            set => _placementList[index] = value;
        }
        public IEnumerator<PlacementInfo> GetEnumerator()
        {
            foreach (var actor in _placementList)
                yield return actor;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
