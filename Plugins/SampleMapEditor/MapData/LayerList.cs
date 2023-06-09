using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedStarLibrary.GameTypes;

namespace RedStarLibrary.MapData
{
    public class LayerList : IEnumerable<LayerConfig>
    {
        private List<LayerConfig> _layerList;

        public LayerList()
        {
            _layerList = new List<LayerConfig>();
        }

        public LayerConfig AddObjectToLayers(PlacementInfo actorPlacement, bool isIgnoreLoaded = false)
        {

            LayerConfig config = _layerList.Find(e => e.LayerName == actorPlacement.LayerConfigName);

            if (config != null)
            {

                if (config.IsLayerLoaded && !isIgnoreLoaded)
                {
                    if (config.IsInfoInLayer(actorPlacement))
                        return config;

                    config = _layerList.Find(e => e.LayerName == actorPlacement.LayerConfigName + $"_ScenarioWhack");

                    if (config == null)
                    {
                        config = new LayerConfig(actorPlacement.LayerConfigName + $"_ScenarioWhack");
                        _layerList.Add(config);
                    }

                }

                if (!config.LayerObjects.Contains(actorPlacement)) 
                    config.LayerObjects.Add(actorPlacement);

                return config;
            }

            config = new LayerConfig(actorPlacement);

            _layerList.Add(config);

            return config;
        }

        public void RemoveObjectFromLayers(PlacementInfo placement)
        {
            LayerConfig info = _layerList.Find(e => e.LayerName == placement.LayerConfigName);

            if (info != null)
                if (info.LayerObjects.Contains(placement))
                    info.LayerObjects.Remove(placement);

        }

        public void SetLayersAsLoaded()
        {
            _layerList.ForEach(e => e.IsLayerLoaded = true);
        }

        public LayerConfig GetLayerByName(string name)
        {
            return _layerList.Find(e => e.LayerName == name);

        }

        public bool IsInfoInAnyLayer(PlacementInfo info)
        {
            var config = _layerList.Find(e => e.LayerName == info.LayerConfigName);

            if (config != null)
                    return config.LayerObjects.Any(e => e == info);

            return false;
        }

        public bool IsInfoInAnyLayer(PlacementInfo info, int scenario)
        {
            var config = _layerList.Find(e => e.LayerName == info.LayerConfigName);

            if (config == null)
                config = _layerList.Find(e => e.LayerName == info.LayerConfigName + $"_Scenario{scenario}");

            if (config != null)
                return config.IsInfoInLayer(info);

            return false;
        }

        public IEnumerator<LayerConfig> GetEnumerator()
        {
            foreach (LayerConfig layer in _layerList)
            {
                yield return layer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
