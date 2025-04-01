using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData
{
    // TODO: use this in other places such as LayerConfig
    public class PlacementList
    {
        public string Name { get; set; } = string.Empty;
        public List<PlacementInfo> Placements { get; set; } = new();

        public PlacementList() { }
        public PlacementList(string name) { Name = name; }

        public void Add(PlacementInfo placement) { Placements.Add(placement); }
        public void Remove(PlacementInfo placement) { Placements.Remove(placement); }
    }
}
