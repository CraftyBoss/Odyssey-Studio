using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData
{
    public class LayerConfig
    {
        /// <summary>
        /// Name of the config.
        /// </summary>
        public string LayerName { get;}
        /// <summary>
        /// Dictionary containing lists of placement infos labeled by List type (AreaList, ObjectList, etc)
        /// </summary>
        public List<PlacementInfo> LayerObjects { get; }
        /// <summary>
        /// Specifies whether or not Layer is currently active in the scene.
        /// </summary>
        public bool IsEnabled;

        /// <summary>
        /// Specifies whether a layer has finished loading all objects.
        /// </summary>
        public bool IsLayerLoaded;
        public LayerConfig(PlacementInfo actorPlacement)
        {
            LayerName = actorPlacement.LayerConfigName;
            LayerObjects = new List<PlacementInfo>() { actorPlacement };
            IsEnabled = true;
        }

        public LayerConfig(string layerName)
        {
            LayerName = layerName;
            LayerObjects = new List<PlacementInfo>();
            IsEnabled = true;
        }
    }
}
