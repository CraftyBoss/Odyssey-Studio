using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData
{
    public class LayerInfo
    {
        public string LayerName { get;}
        public List<int> ScenarioList { get;}
        public Dictionary<string, List<PlacementInfo>> LayerObjects { get;}
        public LayerInfo(PlacementInfo actorPlacement, int scenario, string category)
        {
            LayerName = actorPlacement.LayerName;
            ScenarioList = new List<int>() { scenario };
            LayerObjects = new Dictionary<string, List<PlacementInfo>>() { { category, new List<PlacementInfo>() { actorPlacement } } };
        }
    }
}
