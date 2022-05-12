﻿using System;
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
        public static List<LayerInfo> LayerList { get; set; }

        public static void CreateNewList()
        {
            LayerList = new List<LayerInfo>();
        }

        public static LayerInfo AddObjectToLayers(PlacementInfo actorPlacement, int scenario, string actorCategory)
        {

            if (LayerList == null) 
                return new LayerInfo(actorPlacement, scenario, actorCategory);

            LayerInfo info = LayerList.Find(e => e.LayerName == actorPlacement.LayerName);

            if (info != null)
            {
                if (!info.ScenarioList.Contains(scenario)) info.ScenarioList.Add(scenario);

                if (!info.LayerObjects.ContainsKey(actorCategory))
                    info.LayerObjects.Add(actorCategory, new List<PlacementInfo>() { actorPlacement });
                else
                    if (!info.LayerObjects[actorCategory].Contains(actorPlacement)) info.LayerObjects[actorCategory].Add(actorPlacement);

                return info;
            }

            info = new LayerInfo(actorPlacement, scenario, actorCategory);

            LayerList.Add(info);

            return info;
        }

        public static LayerInfo GetLayerByName(string name)
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

        public static List<string> GetAllLayerNamesInScenario(int scenarioNo)
        {
            return LayerList.Where(e => e.ScenarioList.Contains(scenarioNo)).Select(e => e.LayerName).ToList();
        }
    }
}
