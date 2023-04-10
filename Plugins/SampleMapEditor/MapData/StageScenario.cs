using ByamlExt.Byaml;
using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using RedStarLibrary.Extensions;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData
{
    public class StageScenario : Interfaces.IBymlSerializable
    {
        public int ScenarioIdx { get; private set; }
        /// <summary>
        /// List of Layers containing unique objects for the current layer, separate from the global layer list.
        /// </summary>
        public Dictionary<string, LayerList> ScenarioLayers { get; private set; }

        public Dictionary<string, HashSet<string>> LoadedLayerNames { get; set; }

        private Dictionary<string, LayerList> GlobalLayerList { get; set; }

        private LayerList LinkedObjects { get; set; }

        public StageScenario(Dictionary<string, LayerList> GlobalLayers, int scenarioIdx)
        {
            ScenarioLayers = new Dictionary<string, LayerList>();
            LoadedLayerNames = new Dictionary<string, HashSet<string>>();
            LinkedObjects = new LayerList();

            GlobalLayerList = GlobalLayers;

            ScenarioIdx = scenarioIdx;
        }

        public LayerList GetPlacementLayer(PlacementInfo info, string layerName)
        {

            LayerList actorLayer;

            if (GlobalLayerList.TryGetValue(layerName, out LayerList globalList))
            {
                if (!globalList.IsInfoInAnyLayer(info))
                {
                    LayerConfig layer = globalList.GetLayerByName(info.LayerConfigName);

                    if (layer != null && layer.IsLayerLoaded)
                    {
                        if (!ScenarioLayers.TryGetValue(layerName, out actorLayer))
                        {
                            actorLayer = new LayerList();
                            ScenarioLayers.Add(layerName, actorLayer);
                        }
                    }
                    else
                    {
                        actorLayer = globalList;
                    }
                }
                else
                {
                    actorLayer = globalList;
                }
            }
            else
            {
                actorLayer = new LayerList();
                GlobalLayerList.Add(layerName, actorLayer);
            }

            return actorLayer;
        }

        private void CreateLinkedActors(PlacementInfo info)
        {
            if (info.isUseLinks)
            {
                //foreach (var actorLink in info.Links)
                //{
                //    info.sourceLinks.Add(actorLink.Key, new List<PlacementInfo>());

                //    foreach (Dictionary<string, dynamic> childActorNode in actorLink.Value)
                //    {
                //        PlacementInfo childPlacement = new PlacementInfo(childActorNode);

                //        childPlacement.isLinkedInfo = true;

                //        LayerList list = GetPlacementLayer(childPlacement, childPlacement.UnitConfig.GenerateCategory);

                //        if (!list.IsInfoInAnyLayer(childPlacement))
                //        {
                //            CreateLinkedActors(childPlacement);
                //            list.AddObjectToLayers(childPlacement);
                //        }
                //        else
                //        {
                //            childPlacement = list.GetLayerByName(childPlacement.LayerConfigName).LayerObjects.Find(e => e == childPlacement);
                //        }

                //        if (!childPlacement.destLinks.ContainsKey(actorLink.Key)) childPlacement.destLinks.Add(actorLink.Key, new List<PlacementInfo>());

                //        childPlacement.destLinks[actorLink.Key].Add(info);

                //        info.sourceLinks[actorLink.Key].Add(childPlacement);

                //    }
                //}
            }
        }

        public void DeserializeByml(BymlIter rootNode)
        {
            foreach ((string listName, BymlIter listIter) in rootNode.As<BymlIter>())
            {
                HashSet<string> layerNames = new HashSet<string>();

                foreach (BymlIter actorIter in listIter.AsArray<BymlIter>())
                {
                    PlacementInfo actorPlacement = new PlacementInfo(actorIter);

                    LayerList actorLayer = GetPlacementLayer(actorPlacement, listName);

                    layerNames.Add(actorPlacement.LayerConfigName);

                    if (!actorLayer.IsInfoInAnyLayer(actorPlacement))
                    {
                        // CreateLinkedActors(actorPlacement);

                        actorLayer.AddObjectToLayers(actorPlacement);
                    }
                }

                LoadedLayerNames.Add(listName, layerNames);
            }

            // actor linking

            foreach (var item in LoadedLayerNames)
            {

            }

            foreach (var layerList in GlobalLayerList.Values)
            {
                layerList.SetLayersAsLoaded();
            }
        }

        public BymlContainer SerializeByml()
        {

            BymlHash result = new BymlHash();

            foreach (var layerList in LoadedLayerNames)
            {

                BymlArray layerArr = new BymlArray();

                foreach (var layerName in layerList.Value)
                {
                    foreach (var placement in GlobalLayerList[layerList.Key].First(e => e.LayerName == layerName).LayerObjects)
                    {
                        if (placement.IsLinkDest && placement.destLinks.Count == 0 && placement.sourceLinks.Count == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Stage: {placement.PlacementFileName} Obj ID: {placement.Id} Name: {placement.UnitConfigName} Scenario: {ScenarioIdx} Considered a link Destination!");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }

                        if(placement.ObjectName == "AnimalChaseExGround000")
                        {

                        }

                        if (!placement.isLinkedInfo)
                            layerArr.Add(placement.SerializeByml());

                    }
                }

                result.Add(layerList.Key, layerArr);

            }

            return result;
        }
    }
}
