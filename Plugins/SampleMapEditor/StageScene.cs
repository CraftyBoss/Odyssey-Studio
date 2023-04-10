using System;
using System.Collections.Generic;
using System.Drawing;
using GLFrameworkEngine;
using OpenTK;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using CafeLibrary.Rendering;
using System.IO;
using CafeLibrary;
using Toolbox.Core.IO;
using Toolbox.Core;
using ByamlExt.Byaml;
using RedStarLibrary.GameTypes;
using RedStarLibrary.Rendering.Area;
using ImGuiNET;
using RedStarLibrary.MapData;
using RedStarLibrary.Extensions;
using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using System.Linq;

namespace RedStarLibrary
{
    internal class StageScene : Interfaces.IBymlSerializable
    {
        /// <summary>
        /// Placement data for every actor in the stage
        /// </summary>
        public List<StageScenario> StageScenarios { get; set; } = new List<StageScenario>();
        /// <summary>
        /// Every layer that is shared between scenarios will be used here
        /// </summary>
        public Dictionary<string, LayerList> GlobalLayers { get; set; } = new Dictionary<string, LayerList>();

        /// <summary>
        /// The current Scenario selected for the loaded map.
        /// </summary>
        public static int MapScenarioNo { get; set; } = 0;

        // List Name -> List of Actors in Layers (Layer Common) -> List of Actors
        private Dictionary<string, Dictionary<string,List<LiveActor>>> SceneActors { get; set; }

        public void Setup(EditorLoader loader)
        {
            ProcessLoading.Instance.IsLoading = true;

            //Prepare a collision caster for snapping objects onto
            SetupSceneCollision();
            //Add some objects to the scene
            SetupObjects(loader);

            ProcessLoading.Instance.IsLoading = false;
        }
        public void RestartScene(EditorLoader loader)
        {

            EditorLoader.IsReloadingStage = true;

            for (int i = loader.Scene.Objects.Count - 1; i >= 0; i--)
            {
                loader.RemoveRender(loader.Scene.Objects[i]);
            }

            for (int i = loader.Root.Children.Count - 1; i >= 0; i--)
            {
                // remove actor lists in layer
                loader.Root.Children[i].Children.Clear();

                // remove layer from root
                loader.Root.Children.Remove(loader.Root.Children[i]);
            }

            AddSceneRenders(loader);

            EditorLoader.IsReloadingStage = false;

        }
        public void AddSceneRenders(EditorLoader loader)
        {
            StageScenario curScenarioPlacement = StageScenarios.Find(e => e.ScenarioIdx == MapScenarioNo);

            SceneActors = new Dictionary<string, Dictionary<string, List<LiveActor>>>();

            //if(loader.MapActorList.TryGetValue("Graphics Objects", out List<ActorList> objLists))
            //    foreach (var objList in objLists) combinedList.Add(objList);

            LoadRendersFromList(loader, GlobalLayers, "Global Layer ");

            LoadRendersFromList(loader, curScenarioPlacement.ScenarioLayers, "Scenario Layer ");
        }
        public HashSet<string> GetLoadedLayers()
        {
            HashSet<string> layers = new HashSet<string>();

            StageScenario curScenarioPlacement = StageScenarios.Find(e => e.ScenarioIdx == MapScenarioNo);

            foreach (var layerNames in curScenarioPlacement.LoadedLayerNames)
            {
                foreach (var name in layerNames.Value)
                {
                    layers.Add(name);
                }
            }

            return layers;
        }
        private void SetupObjects(EditorLoader loader)
        {

            // Load Actor Models used in Current Scenario

            AddSceneRenders(loader);

            // Load Skybox

            if (loader.MapGraphicsPreset != null)
            {
                string arcName = loader.MapGraphicsPreset["Sky"]["Name"];

                string skyPath = ResourceManager.FindResourcePath($"ObjectData\\{arcName}.szs");

                var skySarc = ResourceManager.FindOrLoadSARC(skyPath);

                List<ActorList> graphicsObjs = new List<ActorList>();
                ActorList skyboxList = new ActorList("Skybox");
                graphicsObjs.Add(skyboxList);

                NodeBase GraphicsObjs = new NodeBase("Graphics Objects");
                GraphicsObjs.HasCheckBox = true;
                loader.Root.AddChild(GraphicsObjs);
                GraphicsObjs.Icon = IconManager.MODEL_ICON.ToString();

                LiveActor skyActor = new LiveActor(GraphicsObjs, arcName, skyPath);

                skyActor.CreateBfresRenderer(skySarc.GetModelStream(arcName));

                var bfresRender = ((BfresRender)skyActor.ObjectRender);

                bfresRender.UseDrawDistance = false;
                bfresRender.StayInFrustum = true;

                foreach (var tex in bfresRender.Textures.Values)
                    tex.AlphaChannel = STChannelType.One; // disable alpha channel

                skyActor.ObjectRender.CanSelect = false;

                skyboxList.Add(skyActor);

                loader.AddRender(skyActor.ObjectRender);
            }

        }
        private void LoadRendersFromList(EditorLoader loader, Dictionary<string, LayerList> stageLayers, string nodePrefix = "")
        {
            foreach (var objList in CreateActorsFromList(stageLayers))
            {


                NodeBase actorList = loader.Root.GetChild(objList.Key);

                if(actorList == null)
                {
                    actorList = new NodeBase(objList.Key);
                    actorList.HasCheckBox = true;
                    loader.Root.AddChild(actorList);
                    actorList.Icon = IconManager.FOLDER_ICON.ToString();

                    SceneActors.Add(objList.Key, new Dictionary<string, List<LiveActor>>());

                }

                foreach (var mapActors in objList.Value)
                {

                    string nodeName = nodePrefix + mapActors.ActorListName;

                    NodeBase layerActors = actorList.GetChild(nodeName);

                    if(layerActors == null)
                    {
                        layerActors = new NodeBase(nodeName);
                        layerActors.HasCheckBox = true;
                        actorList.AddChild(layerActors);
                        layerActors.Icon = IconManager.FOLDER_ICON.ToString();

                        SceneActors[objList.Key].Add(mapActors.ActorListName, new List<LiveActor>());
                    }

                    foreach (var actor in mapActors)
                    {
                        actor.ResetLinkedActors();

                        actor.SetParentNode(layerActors);

                        if (actor.ObjectDrawer != null)
                            loader.AddRender(actor.ObjectDrawer);
                        else
                            loader.AddRender(actor.ObjectRender);

                        actor.isPlaced = true;

                        SceneActors[objList.Key][mapActors.ActorListName].Add(actor);

                        // actor.PlaceLinkedObjects(loader);

                    }
                }

            }
        }

        private Dictionary<string, List<ActorList>> CreateActorsFromList(Dictionary<string, LayerList> actorList)
        {
            Dictionary<string, List<ActorList>> scenarioList = new Dictionary<string, List<ActorList>>();

            StageScenario curScenarioPlacement = StageScenarios.Find(e => e.ScenarioIdx == MapScenarioNo);

            foreach (var placementList in actorList)
            {

                if(!curScenarioPlacement.LoadedLayerNames.ContainsKey(placementList.Key))
                    continue;

                List<ActorList> layerActors = new List<ActorList>();

                foreach (var config in placementList.Value)
                {

                    if (!curScenarioPlacement.LoadedLayerNames[placementList.Key].Contains(config.LayerName))
                        continue;

                    ActorList actors = new ActorList(config.LayerName);

                    foreach (var placement in config.LayerObjects)
                    {
                        actors.Add(LoadActorFromPlacement(placement));
                    }

                    layerActors.Add(actors);

                }

                scenarioList.Add(placementList.Key, layerActors);
            }

            return scenarioList;
        }
        private LiveActor LoadActorFromPlacement(PlacementInfo placement)
        {
            LiveActor actor = new LiveActor(null, placement);

            if (actor.hasArchive)
            {
                var modelARC = ResourceManager.FindOrLoadSARC(actor.modelPath);

                var modelStream = modelARC.GetModelStream(Path.GetFileNameWithoutExtension(actor.modelPath));

                if (modelStream != null)
                {
                    var fileStream = modelARC.GetInitFileStream("InitClipping");

                    actor.CreateBfresRenderer(modelStream, modelARC.GetTexArchive());

                    if (fileStream != null)
                    {
                        BymlFileData clippingData = ByamlFile.LoadN(modelARC.GetInitFileStream("InitClipping"));

                        if (((Dictionary<string, dynamic>)clippingData.RootNode).TryGetValue("Radius", out dynamic dist))
                        {
                            actor.IsInvalidateClipping = false;
                            actor.ClippingDist = dist;
                        }
                        else if (((Dictionary<string, dynamic>)clippingData.RootNode).TryGetValue("Invalidate", out dynamic isInvalid))
                        {
                            actor.IsInvalidateClipping = clippingData.RootNode["Invalidate"];
                        }
                    }

                    actor.SetActorIcon(Rendering.MapEditorIcons.OBJECT_ICON);
                }
                else
                {
                    actor.CreateBasicRenderer();
                    actor.SetActorIcon(Rendering.MapEditorIcons.OBJECT_ICON);
                }
            }
            else
            {
                if (actor.Placement.ClassName.Contains("Area") && actor.Placement.ModelName != null && actor.Placement.ModelName.StartsWith("Area"))
                {
                    actor.ArchiveName = actor.Placement.ClassName;
                    actor.CreateAreaRenderer();
                    actor.SetActorIcon(Rendering.MapEditorIcons.AREA_BOX);
                }
                else if (actor.Placement.Id.Contains("rail")) // all rails use "rail" instead of "obj" for the id prefix
                {
                    actor.CreateRailRenderer();
                    actor.SetActorIcon(Rendering.MapEditorIcons.POINT_ICON);
                }
                else
                {
                    actor.CreateBasicRenderer();
                    actor.SetActorIcon(Rendering.MapEditorIcons.OBJECT_ICON);
                }
            }

            if(actor.Placement.IsLinkDest)
            {
                actor.SetActorIcon(IconManager.LINK_ICON);

                foreach (var placementList in actor.Placement.destLinks.Values)
                {
                    foreach (var actorPlacement in placementList)
                    {
                        var destActor = LoadActorFromPlacement(actorPlacement);

                        if(destActor != null && destActor.ObjectRender != null && actor.ObjectRender != null)
                        {
                            if (!actor.ObjectRender.DestObjectLinks.Contains(destActor.ObjectRender))
                                actor.ObjectRender.DestObjectLinks.Add(destActor.ObjectRender);

                            if (!destActor.ObjectRender.SourceObjectLinks.Contains(actor.ObjectRender))
                                destActor.ObjectRender.SourceObjectLinks.Add(actor.ObjectRender);
                        }
                    }
                }
            }

            return actor;
        }

        private LiveActor FindActorByPlacement(PlacementInfo info)
        {
            if (SceneActors.TryGetValue(info.UnitConfig.GenerateCategory, out Dictionary<string, List<LiveActor>> list))
            {
                if(list.TryGetValue(info.LayerConfigName, out List<LiveActor> actors))
                {
                    return actors.Find(e => e.Placement == info);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a big plane which you can drop objects onto.
        /// </summary>
        private void SetupSceneCollision()
        {
            var context = GLContext.ActiveContext;

            float size = 2000;
            float height = 0;

            //Make a big flat plane for placing spaces on.
            context.CollisionCaster.Clear();
            context.CollisionCaster.AddTri(
                new Vector3(-size, height, size),
                new Vector3(0, height, -(size * 2)),
                new Vector3(size * 2, height, 0));
            context.CollisionCaster.AddTri(
                new Vector3(-size, height, -size),
                new Vector3(size * 2, height, 0),
                new Vector3(size * 2, height, size * 2));
            context.CollisionCaster.UpdateCache();
        }

        public void DeserializeByml(BymlIter rootNode)
        {
            int scenarioIdx = 0;

            foreach (BymlIter scenarioIter in rootNode.AsArray<BymlIter>())
            {
                StageScenario scenario = new StageScenario(GlobalLayers, scenarioIdx);

                scenario.DeserializeByml(scenarioIter);

                StageScenarios.Add(scenario);

                scenarioIdx++;
            }
        }

        public BymlContainer SerializeByml()
        {

            BymlArray result = new BymlArray();

            foreach (var scenario in StageScenarios)
            {
                result.Add(scenario.SerializeByml());
            }

            return result;
        }
    }
}