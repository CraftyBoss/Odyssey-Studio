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

            LayerConfig info = _layerList.Find(e => e.LayerName == actorPlacement.LayerConfigName);

            if (info != null)
            {

                if (info.IsLayerLoaded && !isIgnoreLoaded)
                {
                    if (IsInfoInLayer(info, actorPlacement))
                        return info;

                    info = _layerList.Find(e => e.LayerName == actorPlacement.LayerConfigName + $"_ScenarioWhack");

                    if (info == null)
                    {
                        info = new LayerConfig(actorPlacement.LayerConfigName + $"_ScenarioWhack");
                        _layerList.Add(info);
                    }

                }

                if (!info.LayerObjects.Contains(actorPlacement)) 
                    info.LayerObjects.Add(actorPlacement);

                return info;
            }

            info = new LayerConfig(actorPlacement);

            _layerList.Add(info);

            return info;
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
                _layerList.Find(e => e.LayerName == info.LayerConfigName + $"_Scenario{scenario}");

            if (config != null)
                return config.LayerObjects.Any(e => e == info);

            return false;
        }

        public static bool IsInfoInLayer(LayerConfig config, PlacementInfo info)
        {
            return config != null && config.LayerObjects.Any(e => e == info);
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
