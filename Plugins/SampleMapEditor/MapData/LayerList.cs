﻿using System;
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

        public LayerConfig AddObjectToLayers(PlacementInfo actorPlacement, int useScenario = -1)
        {
            LayerConfig config = _layerList.Find(e => e.LayerName == actorPlacement.LayerConfigName);

            if (config != null)
            {
                if (!config.LayerObjects.Contains(actorPlacement)) 
                    config.LayerObjects.Add(actorPlacement);

                if(useScenario != -1)
                    config.SetScenarioActive(useScenario, true);

                return config;
            }

            config = new LayerConfig(actorPlacement);

            if (useScenario != -1)
                config.SetScenarioActive(useScenario, true);

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

        public bool IsIdInAnyLayer(PlacementId id)
        {
            var config = _layerList.Find(e => e.LayerName == id.LayerConfigName);

            if (config != null)
                return config.LayerObjects.Any(e => e == id);

            return false;
        }

        public PlacementInfo? GetInfoById(PlacementId id)
        {
            var config = _layerList.Find(e => e.LayerName == id.LayerConfigName);

            if (config != null)
                return config.LayerObjects.Single(e => e == id);

            return null;
        }

        public IEnumerable<string> GetLayerNames() => _layerList.Select(e=> e.LayerName);

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
