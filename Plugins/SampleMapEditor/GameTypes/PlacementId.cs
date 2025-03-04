using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using RedStarLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.GameTypes
{
    /// <summary>
    /// Helper class for getting identification info from a PlacementInfo directly through its byml data
    /// </summary>
    public class PlacementId
    {

        public string Id, UnitConfigName, LayerConfigName;

        public PlacementId(BymlIter rootNode)
        {
            rootNode.TryGetValue("Id", out Id);
            rootNode.TryGetValue("UnitConfigName", out UnitConfigName);
            rootNode.TryGetValue("LayerConfigName", out LayerConfigName);
        }

        public PlacementId(PlacementInfo info)
        {
            Id = info.Id;
            UnitConfigName = info.UnitConfigName;
            LayerConfigName = info.LayerConfigName;
        }

        public static bool operator ==(PlacementId obj1, PlacementId obj2)
        {
            if (ReferenceEquals(obj1, obj2))
                return true;
            if (ReferenceEquals(obj1, null))
                return false;
            if (ReferenceEquals(obj2, null))
                return false;
            return obj1.Equals(obj2);
        }
        public static bool operator !=(PlacementId obj1, PlacementId obj2) => !(obj1 == obj2);
        public bool Equals(PlacementId other)
        {
            return Id == other.Id && UnitConfigName == other.UnitConfigName && LayerConfigName == other.LayerConfigName;
        }
        public override bool Equals(object obj) => Equals(obj as PlacementId);
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, UnitConfigName, LayerConfigName);
        }
    }
}
