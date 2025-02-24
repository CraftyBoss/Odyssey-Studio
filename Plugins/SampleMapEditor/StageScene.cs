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
using RedStarLibrary.Helpers;
using Discord;
using static Toolbox.Core.GX.DisplayListHelper;
using RedStarLibrary.MapData.Graphics;
using static RedStarLibrary.MapData.Graphics.StageGraphicsArea;

namespace RedStarLibrary
{
    public class StageScene : Interfaces.IBymlSerializable
    {
        #region Constants

        public static readonly int SCENARIO_COUNT = 15;

        #endregion

        #region StageData

        /// <summary>
        /// Every layer that is shared between scenarios will be used here
        /// TODO: remove category separation of layers
        /// </summary>
        public Dictionary<string, LayerList> GlobalLayers { get; set; } = new Dictionary<string, LayerList>();
        public StageGraphicsArea GraphicsArea { get; private set; } = new StageGraphicsArea();

        /// <summary>
        /// The current Scenario selected for the loaded map.
        /// </summary>
        public int MapScenarioNo { get; set; } = 0;
        public bool IsUseClipDist { get; set; } = false;

        // Category Name (ex: ObjectList) -> List of Actors in Layers (ex: Layer Common) -> List of Actors
        private Dictionary<string, Dictionary<string,List<LiveActor>>> SceneActors { get; set; }

        public int CurrentObjectID = 0;

        #endregion

        #region Misc

        #endregion

        public void Setup(PlacementFileEditor loader, bool isNew = false)
        {
            ProcessLoading.Instance.IsLoading = true;

            //Prepare a collision caster for snapping objects onto
            SetupSceneCollision();

            //Add some objects to the scene
            SetupObjects(loader);

            // if the scene is being loaded as a new stage, add in some basic data to make the stage loadable in-game
            if (isNew)
            {
                // add common layer with all scenarios enabled
                var layerList = GetOrCreateLayerConfig("PlayerList", "Common");
                layerList.SetAllScenarioActive(true);

                TryAddActorByName(loader, "PlayerActorHakoniwa", category: "Player");
                GraphicsArea.AddNewParam();
            }

            var playerObj = FindActorWithClass("PlayerList", "PlayerActorHakoniwa");
            // set target position for current camera to the player
            if (playerObj != null)
            {
                var ctxCamera = GLContext.ActiveContext.Camera;
                var forwardDir = playerObj.Transform.Rotation * Vector3.UnitZ;
                var rot = playerObj.Transform.RotationEulerDegrees;
                float offset = 500.0f;

                ctxCamera.TargetPosition = playerObj.Transform.Position + (-forwardDir * offset) + (Vector3.UnitY * offset);

                ctxCamera.RotationDegreesX = rot.X;
                ctxCamera.RotationDegreesY = 180.0f - rot.Y;
                ctxCamera.RotationDegreesZ = rot.Z;
            }

            ProcessLoading.Instance.IsLoading = false;
        }

        public void LoadGraphicsArea(BymlIter iter) => GraphicsArea.DeserializeByml(iter);

        public void RestartScene(PlacementFileEditor loader)
        {
            PlacementFileEditor.IsLoadingStage = true;

            for (int i = loader.Scene.Objects.Count - 1; i >= 0; i--)
                loader.RemoveRender(loader.Scene.Objects[i]);

            for (int i = loader.Root.Children.Count - 1; i >= 0; i--)
            {
                // remove actor lists in layer
                loader.Root.Children[i].Children.Clear();

                // remove layer from root
                loader.Root.Children.Remove(loader.Root.Children[i]);
            }

            AddSceneRenders(loader);

            TryCreateGraphicsDataRenders(loader);

            PlacementFileEditor.IsLoadingStage = false;
        }

        public void AddSceneRenders(PlacementFileEditor loader)
        {
            SceneActors = new Dictionary<string, Dictionary<string, List<LiveActor>>>();

            LoadRendersFromList(loader);
        }

        public void AddActorFromAsset(PlacementFileEditor loader, Vector3 spawnPos, AssetMenu.LiveActorAsset asset)
        {
            string objCategory = asset.ActorCategory + "List";
            string actorLayer = PlacementFileEditor.SelectedLayer;

            PlacementInfo actorPlacement = new PlacementInfo(asset.DatabaseEntry, asset.Name);

            actorPlacement.LayerConfigName = actorLayer;
            actorPlacement.PlacementFileName = loader.PlacementFileName;
            actorPlacement.Id = "obj" + GetNextAvailableObjID();
            actorPlacement.Translate = spawnPos;

            LayerList categoryLayers = GetOrCreateLayerList(objCategory);

            LayerConfig placementLayer = categoryLayers.AddObjectToLayers(actorPlacement, MapScenarioNo);
            actorPlacement.SetActiveScenarios(placementLayer);

            NodeBase actorList = loader.Root.GetChild(objCategory);

            if (actorList == null)
            {
                actorList = new NodeBase(objCategory);
                actorList.HasCheckBox = true;
                loader.Root.AddChild(actorList);
                actorList.Icon = IconManager.FOLDER_ICON.ToString();

                SceneActors.Add(objCategory, new Dictionary<string, List<LiveActor>>());
            }

            NodeBase layerActors = actorList.GetChild("Layer " + actorLayer);

            if (layerActors == null)
                layerActors = AddLayerToActorList(loader, actorList, actorLayer, objCategory);

            LiveActor actor = LoadActorFromPlacement(actorPlacement, placementLayer);

            if(actor.ObjectRender is TransformableObject)
                actor.Placement.ModelName = ""; // if we didn't find a model to load, clear the model name

            actor.ResetLinkedActors();

            actor.SetParentNode(layerActors);

            if (actor.ObjectDrawer != null)
                loader.AddRender(actor.ObjectDrawer);
            else
                loader.AddRender(actor.ObjectRender);

            actor.actorLayer = placementLayer;

            actor.isPlaced = true;

            SceneActors[objCategory][actorLayer].Add(actor);

        }

        public bool TryAddActorByName(PlacementFileEditor loader, string className, Vector3 spawnPos = new Vector3(), string category = null)
        {
            var databaseEntry = ActorDataBase.GetObjectFromDatabase(className, category);
            if (databaseEntry == null)
                return false;

            AddActorFromAsset(loader, spawnPos, new AssetMenu.LiveActorAsset(databaseEntry, className));

            return true;
        }

        public HashSet<string> GetLoadedLayers()
        {
            HashSet<string> layers = new HashSet<string>();

            foreach ((var objCategory, var layerList) in GlobalLayers)
            {
                foreach (var name in layerList.GetLayerNames())
                    layers.Add(name);
            }

            return layers;
        }

        public LayerList GetOrCreateLayerList(string objCategory)
        {
            if(!GlobalLayers.TryGetValue(objCategory, out LayerList layerList))
                GlobalLayers.Add(objCategory, layerList = new LayerList());
            return layerList;
        }

        public LayerConfig GetOrCreateLayerConfig(string objCategory, string layerName, int useScenario = -1) 
        {
            var layerList = GetOrCreateLayerList(objCategory);

            return layerList.FindOrCreateLayer(layerName, useScenario);
        }

        public List<LiveActor> GetLoadedActors()
        {
            List<LiveActor> liveActors = new List<LiveActor>();

            foreach (var categoryList in SceneActors)
                foreach (var layerList in categoryList.Value)
                    liveActors.AddRange(layerList.Value);

            return liveActors;
        }

        public LiveActor? FindActorWithClass(string objCategory, string className)
        {
            if(!SceneActors.TryGetValue(objCategory, out var actorLists))
                return null;

            return actorLists.First().Value.FirstOrDefault(e=> e.Placement.ClassName == className);
        }

        private NodeBase AddLayerToActorList(PlacementFileEditor loader, NodeBase actorList, string actorLayer, string objCategory)
        {
            string nodeName = "Layer " + actorLayer;

            var layerActors = new NodeBase(nodeName);
            layerActors.HasCheckBox = true;
            layerActors.Tag = loader;
            actorList.AddChild(layerActors);
            layerActors.Icon = IconManager.FOLDER_ICON.ToString();

            layerActors.ContextMenus.Add(new MenuItemModel("Clear", () =>
            {
                for (int x = layerActors.Children.Count - 1; x >= 0; x--)
                {
                    var actorNode = layerActors.Children[x];

                    if (actorNode is EditableObjectNode editNode)
                        loader.RemoveRender(editNode.Object);
                }

                if (layerActors.Children.Count == 0)
                    actorList.Children.Remove(layerActors);
            }));

            SceneActors[objCategory].TryAdd(actorLayer, new List<LiveActor>());

            return layerActors;
        }

        private void SetupObjects(PlacementFileEditor loader)
        {
            // Load Actor Models used in Current Scenario
            AddSceneRenders(loader);

            // Load Skybox
            TryCreateGraphicsDataRenders(loader);
        }

        private void LoadRendersFromList(PlacementFileEditor loader)
        {
            foreach ((string objCategory, List<ActorList> actorLists) in CreateActorsFromList(GlobalLayers))
            {
                NodeBase actorList = loader.Root.GetChild(objCategory);

                if (actorList == null)
                {
                    actorList = new NodeBase(objCategory);
                    actorList.Tag = loader.Root.Tag;
                    actorList.HasCheckBox = true;
                    loader.Root.AddChild(actorList);
                    actorList.Icon = IconManager.FOLDER_ICON.ToString();

                    actorList.ContextMenus.Add(new MenuItemModel("Clear", () =>
                    {
                        for (int i = actorList.Children.Count - 1; i >= 0; i--)
                        {
                            var layerActors = actorList.Children[i];

                            for (int x = layerActors.Children.Count - 1; x >= 0; x--)
                            {
                                var actorNode = layerActors.Children[x];

                                if (actorNode is EditableObjectNode editNode)
                                    loader.RemoveRender(editNode.Object);
                            }

                            if(layerActors.Children.Count == 0)
                                actorList.Children.Remove(layerActors);
                        }
                    }));

                    SceneActors.Add(objCategory, new Dictionary<string, List<LiveActor>>());
                }

                foreach (var mapActors in actorLists)
                {
                    string nodeName = "Layer " + mapActors.ActorListName;

                    NodeBase layerActors = actorList.GetChild(nodeName);

                    if (layerActors == null)
                        layerActors = AddLayerToActorList(loader, actorList, mapActors.ActorListName, objCategory);

                    foreach (var actor in mapActors)
                    {
                        actor.ResetLinkedActors();

                        actor.SetParentNode(layerActors);

                        if (actor.ObjectDrawer != null)
                            loader.AddRender(actor.ObjectDrawer);
                        else
                            loader.AddRender(actor.ObjectRender);

                        actor.isPlaced = true;

                        SceneActors[objCategory][mapActors.ActorListName].Add(actor);

                        // actor.PlaceLinkedObjects(loader);
                    }
                }
            }
        }

        private void TryCreateGraphicsDataRenders(PlacementFileEditor loader)
        {
            if (GraphicsArea == null)
                return;

            var areaParam = GraphicsArea.TryGetScenarioParam(MapScenarioNo);
            if (areaParam == null)
                areaParam = GraphicsArea.TryGetDefaultAreaParam();

            if (areaParam == null)
                return;

            if (!loader.TryGetPresetFromArc(areaParam.PresetName, out var presetData))
                return;

            string arcName = presetData["Sky"]["Name"];

            string skyPath = ResourceManager.FindResourcePath($"ObjectData\\{arcName}.szs");

            if (!File.Exists(skyPath))
                return;

            var skySarc = ResourceManager.FindOrLoadSARC(skyPath);

            List<ActorList> graphicsObjs = new List<ActorList>();
            ActorList skyboxList = new ActorList("Skybox");
            graphicsObjs.Add(skyboxList);

            NodeBase GraphicsObjs = new NodeBase("Graphics Objects");
            GraphicsObjs.Tag = loader.Root.Tag;
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

        private Dictionary<string, List<ActorList>> CreateActorsFromList(Dictionary<string, LayerList> actorList)
        {
            Dictionary<string, List<ActorList>> scenarioList = new Dictionary<string, List<ActorList>>();

            foreach ((var objCategory, var layerList) in actorList)
            {
                if (!scenarioList.TryGetValue(objCategory, out List<ActorList> layerActors))
                    scenarioList.Add(objCategory, layerActors = new List<ActorList>());

                foreach (var layer in layerList)
                {
                    ActorList actors = new ActorList(layer.LayerName);

                    foreach (var placement in layer.LayerObjects)
                    {
                        if(placement.IsScenarioActive(MapScenarioNo))
                            actors.Add(LoadActorFromPlacement(placement, layer));
                    }

                    if(actors.Any())
                        layerActors.Add(actors);
                }
            }

            return scenarioList;

            //StageScenario curScenarioPlacement = GetCurrentScenario();
            //foreach (var placementList in actorList)
            //{
            //    if(!curScenarioPlacement.LoadedLayerNames.ContainsKey(placementList.Key))
            //        continue;
            //    if(!scenarioList.TryGetValue(placementList.Key, out List<ActorList> layerActors))
            //    {
            //        layerActors = new List<ActorList>();
            //        scenarioList.Add(placementList.Key, layerActors);
            //    }
            //    foreach (var config in placementList.Value)
            //    {
            //        if (!curScenarioPlacement.LoadedLayerNames[placementList.Key].Contains(config.LayerName))
            //            continue;
            //        ActorList actors = new ActorList(config.LayerName);
            //        foreach (var placement in config.LayerObjects)
            //            actors.Add(LoadActorFromPlacement(placement, config));
            //        layerActors.Add(actors);
            //    }
            //}
            //return scenarioList;
        }

        private LiveActor LoadActorFromPlacement(PlacementInfo placement, LayerConfig list = null)
        {
            LiveActor actor = new LiveActor(null, placement);

            actor.actorLayer = list;

            if (actor.hasArchive)
            {
                actor.TryLoadModelRenderer();
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

            if (actor.Placement.IsLinkDest)
            {
                actor.SetActorIcon(IconManager.LINK_ICON);

                foreach (var placementList in actor.Placement.destLinks.Values)
                {
                    foreach (var actorPlacement in placementList)
                    {
                        var destActor = LoadActorFromPlacement(actorPlacement);

                        if (destActor != null && destActor.ObjectRender != null && actor.ObjectRender != null)
                        {
                            if (!actor.ObjectRender.DestObjectLinks.Contains(destActor.ObjectRender))
                                actor.ObjectRender.DestObjectLinks.Add(destActor.ObjectRender);

                            if (!destActor.ObjectRender.SourceObjectLinks.Contains(actor.ObjectRender))
                                destActor.ObjectRender.SourceObjectLinks.Add(actor.ObjectRender);
                        }
                    }
                }
            }

            if(actor.Placement.ClassName.Contains("Camera"))
            {

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

        private int GetNextAvailableObjID() => CurrentObjectID++;  

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
                DeserializeScenario(scenarioIter, scenarioIdx++);

            // after finding the highest objid, increment it so we can use it for the next object placed down
            CurrentObjectID++;
        }

        public BymlContainer SerializeByml()
        {
            BymlArray result = new BymlArray();

            for (int i = 0; i < SCENARIO_COUNT; i++)
            {
                var scenarioByml = new BymlHash();

                foreach ((var objCategory, var layerList) in GlobalLayers)
                {
                    var listByml = new BymlArray();

                    foreach (var config in layerList)
                    {
                        foreach (var placement in config.GetPlacementsInScenario(i))
                            listByml.Add(placement.SerializeByml());
                    }

                    if(listByml.Length() > 0)
                        scenarioByml.Add(objCategory, listByml);
                }

                result.Add(scenarioByml);
            }

            return result;
        }

        private void DeserializeScenario(BymlIter rootNode, int scenarioIdx)
        {
            foreach ((string objCategory, BymlIter listIter) in rootNode.As<BymlIter>())
            {
                HashSet<string> layerNames = new HashSet<string>();

                foreach (BymlIter actorIter in listIter.AsArray<BymlIter>())
                {
                    PlacementId actorId = new PlacementId(actorIter);

                    if (!GlobalLayers.TryGetValue(objCategory, out LayerList globalList))
                        GlobalLayers.Add(objCategory, globalList = new LayerList());

                    PlacementInfo actorInfo;
                    if (globalList.IsIdInAnyLayer(actorId))
                    {
                        actorInfo = globalList.GetInfoById(actorId);

                        globalList.GetLayerByName(actorId.LayerConfigName).SetScenarioActive(scenarioIdx, true);
                    }
                    else
                        globalList.AddObjectToLayers(actorInfo = new PlacementInfo(actorIter), scenarioIdx);

                    int objId = int.Parse(actorInfo.Id.Substring(3));
                    if (CurrentObjectID < objId)
                        CurrentObjectID = objId;

                    actorInfo.SetScenarioActive(scenarioIdx, true);

                    layerNames.Add(actorId.LayerConfigName);
                }
            }

            // actor linking

            //foreach (var item in LoadedLayerNames)
            //{

            //}
        }
    }
}