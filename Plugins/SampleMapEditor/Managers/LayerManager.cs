using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedStarLibrary.GameTypes;
using RedStarLibrary.MapData;

namespace RedStarLibrary
{
    public class LayerManager
    {
        public static List<LayerConfig> LayerList { get; set; }

        public static void CreateNewList()
        {
            LayerList = new List<LayerConfig>();
        }

        public static bool CreateNewConfig(string layerName, int scenario)
        {

            if (LayerList.Exists(e => e.LayerName == layerName)) return false;

            LayerList.Add(new LayerConfig(scenario, layerName));

            return true;
        }

        public static LayerConfig AddObjectToLayers(PlacementInfo actorPlacement, int scenario, string actorCategory, bool isIgnoreLoaded = false)
        {

            if (LayerList == null) 
                return new LayerConfig(actorPlacement, scenario, actorCategory);

            LayerConfig info = LayerList.Find(e => e.LayerName == actorPlacement.LayerConfigName);

            if (info != null)
            {

                if (info.IsLayerLoaded && !isIgnoreLoaded)
                {
                    if(IsInfoInLayer(info, actorPlacement))
                        return info;

                    info = LayerList.Find(e => e.LayerName == actorPlacement.LayerConfigName + $"_Scenario{scenario}");

                    if (info == null)
                    {
                        info = new LayerConfig(scenario, actorPlacement.LayerConfigName + $"_Scenario{scenario}");
                        LayerList.Add(info);
                    }
                    
                }

                if (!info.ScenarioList.Contains(scenario))
                    info.ScenarioList.Add(scenario);

                if (!info.LayerObjects.ContainsKey(actorCategory))
                    info.LayerObjects.Add(actorCategory, new List<PlacementInfo>() { actorPlacement });
                else
                    if (!info.LayerObjects[actorCategory].Contains(actorPlacement)) info.LayerObjects[actorCategory].Add(actorPlacement);

                return info;
            }

            info = new LayerConfig(actorPlacement, scenario, actorCategory);

            LayerList.Add(info);

            return info;
        }

        public static void RemoveObjectFromLayers(PlacementInfo placement)
        {
            LayerConfig info = LayerList.Find(e => e.LayerName == placement.LayerConfigName);
            string actorCategory = placement.UnitConfig.GenerateCategory;

            if (info != null)
                if (info.LayerObjects.ContainsKey(actorCategory))
                    if (info.LayerObjects[actorCategory].Contains(placement)) 
                        info.LayerObjects[actorCategory].Remove(placement);

        }
        public static void SetLayersAsLoaded()
        {
            LayerList.ForEach(e => e.IsLayerLoaded = true);
        }
        public static bool AddScenarioToLayer(string layerName, int scenario)
        {
            LayerConfig layer = LayerList.Find(e => e.LayerName == layerName);

            if (layer == null) 
                return false;

            if (!layer.ScenarioList.Contains(scenario))
                layer.ScenarioList.Add(scenario);
            else
                return false;

            return true;
        }

        public static bool RemoveScenarioFromLayer(string layerName, int scenario)
        {
            LayerConfig layer = LayerList.Find(e => e.LayerName == layerName);

            if (layer == null)
                return false;

            if (layer.ScenarioList.Contains(scenario))
                layer.ScenarioList.Remove(scenario);
            else
                return false;

            return true;
        }

        public static LayerConfig GetLayerByName(string name)
        {
            if (LayerList == null) 
                return null;

            return LayerList.Find(e => e.LayerName == name);

        }

        public static Dictionary<string, List<PlacementInfo>> GetAllObjectsInScenario(int scenarioNo)
        {

            if (LayerList == null)
                return null;

            Dictionary<string, List<PlacementInfo>> combinedList = new Dictionary<string, List<PlacementInfo>>();

            foreach (var layer in LayerList.Where(e=> e.ScenarioList.Contains(scenarioNo)))
            {
                foreach (var categoryList in layer.LayerObjects)
                {
                    if (!combinedList.ContainsKey(categoryList.Key))
                        combinedList.Add(categoryList.Key, categoryList.Value);
                    else
                        combinedList[categoryList.Key].AddRange(categoryList.Value);
                }
            }

            return combinedList;
        }

        public static List<LayerConfig> GetLayersInScenario(int scenarioNo)
        {
            return LayerList.Where(e => e.ScenarioList.Contains(scenarioNo)).ToList();
        }

        public static List<LayerConfig> GetLayersNotInScenario(int scenarioNo)
        {
            return LayerList.Where(e => !e.ScenarioList.Contains(scenarioNo)).ToList();
        }

        public static List<string> GetNamesInScenario(int scenarioNo)
        {
            return LayerList.Where(e => e.ScenarioList.Contains(scenarioNo)).Select(e => e.LayerName).ToList();
        }

        public static List<string> GetNamesNotInScenario(int scenarioNo)
        {
            return LayerList.Where(e => !e.ScenarioList.Contains(scenarioNo)).Select(e => e.LayerName).ToList();
        }

        public static bool IsInfoInAnyLayer(PlacementInfo info)
        {
            var config = LayerList.Find(e => e.LayerName == info.LayerConfigName);

            if(config != null)
                if (config.LayerObjects.TryGetValue(info.UnitConfig.GenerateCategory, out List<PlacementInfo> list))
                    return list.Any(e => e == info);

            return false;
        }

        public static bool IsInfoInAnyLayer(PlacementInfo info, int scenario)
        {
            var config = LayerList.Find(e => e.LayerName == info.LayerConfigName);

            if (config == null) 
                LayerList.Find(e => e.LayerName == info.LayerConfigName + $"_Scenario{scenario}");

            if (config != null)
                if (config.LayerObjects.TryGetValue(info.UnitConfig.GenerateCategory, out List<PlacementInfo> list))
                    return list.Any(e => e == info);

            return false;
        }

        public static bool IsInfoInLayer(LayerConfig config, PlacementInfo info)
        {
            if (config != null && config.LayerObjects.TryGetValue(info.UnitConfig.GenerateCategory, out List<PlacementInfo> list))
                return list.Any(e => e == info);

            return false;
        }
    }
}
