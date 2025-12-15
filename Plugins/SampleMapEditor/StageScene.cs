using System;
using System.Collections.Generic;
using GLFrameworkEngine;
using OpenTK;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using CafeLibrary.Rendering;
using System.IO;
using CafeLibrary;
using Toolbox.Core.IO;
using Toolbox.Core;
using RedStarLibrary.GameTypes;
using RedStarLibrary.MapData;
using RedStarLibrary.Extensions;
using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using System.Linq;
using RedStarLibrary.MapData.Graphics;
using static Toolbox.Core.Runtime;
using RedStarLibrary.Helpers;

namespace RedStarLibrary
{
    public class StageScene : Interfaces.IBymlSerializable
    {
        #region Constants

        public static readonly int SCENARIO_COUNT = 15;

        #endregion

        #region StageData

        /// <summary>
        /// Dictionary of Layers with the placement list's name as a key. Each entry has a LayerList object that contains a list of layerconfigs that has every placement that belongs to that layer.
        /// TODO: this organization could be improved, its a little confusing at the moment and can be streamlined.
        /// </summary>
        public Dictionary<string, LayerList> GlobalLayers { get; set; } = new Dictionary<string, LayerList>();
        public StageGraphicsArea GraphicsArea { get; private set; } = new StageGraphicsArea();

        /// <summary>
        /// The current Scenario selected for the loaded map.
        /// </summary>
        public int MapScenarioNo { get; set; } = 0;
        public int CurrentObjectID { get; private set; } = 0;
        public bool IsUseClipDist { get; set; } = false;
        public string SelectedLayer { get; set; } = "Common";
        public string StageSuffix { get; set; } = "";

        /// <summary>
        /// Category Name (ex: ObjectList) -> List of Actors in Layers (ex: Layer Common) -> List of Actors
        /// </summary>
        private Dictionary<string, Dictionary<string,List<LiveActor>>> SceneActors { get; set; } = new();
        private ActorList LinkActors { get; set; } = new("LinkedObjects");
        private Dictionary<string, StageScene> MapZones { get; set; } = new();

        #endregion

        #region Misc

        public NodeBase LinkTarget = null;

        private List<LiveActor> copyBuffer = new();

        private NodeBase RootNode;

        private NodeBase LinkedActorsNode;

        #endregion

        public static StageScene LoadStage(SARC mapArc, string stageName)
        {
            var stage = new StageScene();
            stage.DeserializeByml(new BymlIter(mapArc.GetFileStream(stageName + ".byml").ToArray()));

            return stage;
        }

        public static StageScene LoadStage(Stream stream, string stageName)
        {
            var mapArc = new SARC();
            mapArc.Load(stream);

            return LoadStage(mapArc, stageName);
        }

        public static StageScene LoadStage(string stageName)
        {
            var stagePath = ResourceManager.FindResourcePath(Path.Combine("StageData", stageName + ".szs"));

            if (!File.Exists(stagePath))
                return null;

            return LoadStage(new MemoryStream(YAZ0.Decompress(stagePath)), stageName);
        }

        public void Setup(PlacementFileEditor loader, bool isNew = false)
        {
            ProcessLoading.Instance.IsLoading = true;

            RootNode = new NodeBase(StageSuffix + " Data");
            RootNode.Tag = loader.Root.Tag;
            RootNode.HasCheckBox = true;
            RootNode.IsExpanded = true;
            RootNode.Icon = IconManager.FOLDER_ICON.ToString();

            loader.Root.AddChild(RootNode);

            TryLoadStageZones(loader);

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

        private void TryLoadStageZones(PlacementFileEditor editor)
        {
            if(GlobalLayers.TryGetValue("ZoneList", out var layerList))
            {
                foreach (var layerConfig in layerList)
                {
                    foreach (var zonePlacement in layerConfig.LayerObjects)
                    {
                        var zoneStage = LoadStage(zonePlacement.ObjectName + "Map");
                        if (zoneStage != null)
                            MapZones.Add(zonePlacement.ObjectName, zoneStage);
                    }
                }
            }
        }

        public void LoadGraphicsArea(BymlIter iter) => GraphicsArea.DeserializeByml(iter);

        public void RestartScene(PlacementFileEditor editor)
        {
            PlacementFileEditor.IsLoadingStage = true;

            ResetStage(editor);

            AddSceneRenders(editor);

            TryCreateGraphicsDataRenders(editor);

            PlacementFileEditor.IsLoadingStage = false;
        }

        public void ResetStage(PlacementFileEditor editor)
        {
            for (int i = editor.Scene.Objects.Count - 1; i >= 0; i--)
            {
                var obj = editor.Scene.Objects[i];

                obj.Dispose();

                editor.RemoveRender(obj);
            }

            for (int i = RootNode.Children.Count - 1; i >= 0; i--)
            {
                // remove actor lists in layer
                RootNode.Children[i].Children.Clear();

                // remove layer from root
                RootNode.Children.Remove(RootNode.Children[i]);
            }

            // remove linked actor renderers
            LinkedActorsNode.Children.Clear();
            LinkActors.Clear();

            // clear actor list
            SceneActors.Clear();

            // run collector to clean up any resources no longer in use
            GC.Collect();
        }

        public void AddSceneRenders(PlacementFileEditor loader)
        {
            LinkedActorsNode = CreateCategoryNode(loader, "Linked Objects", icon: IconManager.LINK_ICON);

            LoadRendersFromList(loader);
        }

        public LiveActor AddActorFromAsset(PlacementFileEditor loader, Vector3 spawnPos, AssetMenu.LiveActorAsset asset)
        {
            var assetName = asset.Name;

            if(asset.DatabaseEntry.ActorCategory == "Area")
                assetName = "AreaCubeBase";

            PlacementInfo actorPlacement = new PlacementInfo(asset.DatabaseEntry, assetName);
            actorPlacement.Translate = spawnPos;

            return AddActorFromPlacementInfo(loader, actorPlacement, SelectedLayer);
        }

        public bool TryAddActorByName(PlacementFileEditor loader, string className, Vector3 spawnPos = new Vector3(), string category = null)
        {
            var databaseEntry = ActorDataBase.GetObjectFromDatabase(className, category);
            if (databaseEntry == null)
                return false;

            var assetName = className;
            if (databaseEntry.ActorCategory == "Area")
                assetName = "AreaCubeBase";

            var placementInfo = new PlacementInfo(databaseEntry, assetName);
            placementInfo.Translate = spawnPos;

            AddActorFromPlacementInfo(loader, placementInfo);

            return true;
        }

        public LiveActor AddActorFromPlacementInfo(PlacementFileEditor loader, PlacementInfo actorPlacement, string actorLayer = "")
        {
            if (string.IsNullOrWhiteSpace(actorLayer))
                actorLayer = SelectedLayer;

            var objCategory = actorPlacement.UnitConfig.GenerateCategory;

            actorPlacement.LayerConfigName = SelectedLayer;
            actorPlacement.PlacementFileName = loader.PlacementFileName;
            actorPlacement.objectIndex = GetNextAvailableObjID();
            actorPlacement.Id = "obj" + actorPlacement.objectIndex;
            actorPlacement.IsLinkDest = LinkTarget != null;

            LiveActor actor;
            if(actorPlacement.IsLinkDest)
                actor = CreateLinkedActor(actorPlacement);
            else
                actor = CreatePlacementActor(loader, objCategory, actorPlacement, actorLayer);

            actor.SetupRenderer();

            actor.isPlaced = true;

            loader.AddRender(actor.GetDrawer());

            LinkTarget = null;

            return actor;
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

        public List<LiveActor> GetLoadedActors(bool getLinked = false)
        {
            List<LiveActor> liveActors = new List<LiveActor>();

            void RecursiveCollect(IEnumerable<LiveActor> actors)
            {
                foreach (var actor in actors)
                {
                    if (liveActors.Contains(actor))
                        continue;

                    liveActors.Add(actor);

                    if (getLinked && actor.Placement.isUseLinks)
                    {
                        foreach (var linkActorList in actor.linkedObjs)
                            RecursiveCollect(linkActorList);
                    }
                }
            }

            foreach (var categoryList in SceneActors)
                foreach (var layerList in categoryList.Value)
                    RecursiveCollect(layerList.Value);

            return liveActors;
        }

        public List<PlacementInfo> GetLoadedPlacementInfos()
        {
            List<PlacementInfo> infos = new();

            foreach (var categoryList in GlobalLayers)
                foreach (var layerList in categoryList.Value)
                    infos.AddRange(layerList.LayerObjects);

            return infos;
        }

        public List<LiveActor> GetSelectedActors() => GetActorsByPredicate(e => e.IsRendererSelected());

        public List<LiveActor> GetActorsByClassName(string className) => GetActorsByPredicate(e => e.Placement.ClassName == className);

        public List<LiveActor> GetActorsByPredicate(Predicate<LiveActor> pred)
        {
            List<LiveActor> liveActors = new List<LiveActor>();

            foreach (var categoryList in SceneActors)
                foreach (var layerList in categoryList.Value)
                    liveActors.AddRange(layerList.Value.Where(e => pred(e)));

            return liveActors;
        }

        public List<PlacementInfo> GetPlacementByPredicate(Predicate<PlacementInfo> pred)
        {
            List<PlacementInfo> infos = new();

            foreach (var categoryList in GlobalLayers)
                foreach (var layerList in categoryList.Value)
                    infos.AddRange(layerList.LayerObjects.Where(e => pred(e)));

            return infos;
        }

        public StageScene TryGetZone(string name)
        {
            if (MapZones.TryGetValue(name, out var zone))
                return zone;
            return null;
        }

        public LiveActor? FindActorWithClass(string objCategory, string className)
        {
            if(!SceneActors.TryGetValue(objCategory, out var actorLists))
                return null;

            return actorLists.First().Value.FirstOrDefault(e=> e.Placement.ClassName == className);
        }

        public void CopySelectedActors(List<LiveActor> buffer = null)
        {
            if (buffer == null)
                buffer = copyBuffer;

            buffer.Clear();

            foreach (var actor in GetSelectedActors())
                buffer.Add(actor);
        }

        public void PasteActorCopyBuffer(PlacementFileEditor editor, List<LiveActor> buffer = null)
        {
            if (buffer == null)
                buffer = copyBuffer;

            GLContext.ActiveContext.Scene.DeselectAll(GLContext.ActiveContext);

            foreach (var actor in buffer)
            {
                var copyActor = actor.Clone();

                copyActor.Placement.objectIndex = GetNextAvailableObjID(); // update objId with new one
                copyActor.Placement.Id = "obj" + copyActor.Placement.objectIndex;

                editor.AddRender(copyActor.GetDrawer());

                copyActor.GetTransformObj().IsSelected = true;

                SceneActors[copyActor.Placement.UnitConfig.GenerateCategory][copyActor.Placement.LayerConfigName].Add(copyActor);
            }
        }

        public LiveActor GetOrCreateLinkActor(PlacementInfo info)
        {
            var actor = LinkActors.GetActorByPlacement(info);
            if (actor != null)
                return actor;

            actor = new LiveActor(null, info);

            actor.LoadLinks(this);

            actor.SetParentNode(LinkedActorsNode);

            actor.SetupRenderer();

            LinkActors.Add(actor);

            return actor;
        }

        private LiveActor CreatePlacementActor(PlacementFileEditor loader, string objCategory, PlacementInfo actorPlacement, string actorLayer)
        {
            LayerList categoryLayers = GetOrCreateLayerList(objCategory);

            LayerConfig placementLayer = categoryLayers.AddObjectToLayers(actorPlacement, MapScenarioNo);
            actorPlacement.SetActiveScenarios(placementLayer);

            NodeBase actorList = RootNode.GetChild(objCategory);

            if (actorList == null)
            {
                actorList = new NodeBase(objCategory);
                actorList.HasCheckBox = true;
                RootNode.AddChild(actorList);
                actorList.Icon = IconManager.FOLDER_ICON.ToString();

                SceneActors.Add(objCategory, new Dictionary<string, List<LiveActor>>());
            }

            NodeBase layerActors = actorList.GetChild("Layer " + actorLayer);

            if (layerActors == null)
                layerActors = AddLayerToActorList(loader, actorList, actorLayer, objCategory);

            LiveActor actor = LoadActorFromPlacement(actorPlacement, placementLayer);

            actor.SetParentNode(layerActors);

            actor.ResetLinkedActors();

            SceneActors[objCategory][SelectedLayer].Add(actor);

            return actor;
        }

        private LiveActor CreateLinkedActor(PlacementInfo info)
        {
            // Note for next session: it might be best to entirely remove linked actors from being viewable in the outliner through child nodes,
            // and instead have a new window for displaying the actors links.

            var actor = new LiveActor(null, info);
            var parentActor = LinkTarget.Parent.Tag as LiveActor;

            actor.SetParentNode(LinkedActorsNode);
            actor.SetupRenderer();

            actor.Placement.SetActiveScenarios(parentActor.Placement);

            actor.Transform.Position = parentActor.Transform.Position;

            LinkActors.Add(actor);

            var actorList = LinkTarget.Tag as ActorList;

            actorList.Add(actor);

            LinkTarget.AddChild(actor.GetRenderNode().UINode);

            return actor;
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
                NodeBase actorList = RootNode.GetChild(objCategory);

                if (actorList == null)
                {
                    actorList = CreateCategoryNode(loader, objCategory);

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
                        actor.SetParentNode(layerActors);

                        actor.SetupRenderer();

                        actor.PlaceLinkedObjects(loader);

                        loader.AddRender(actor.GetDrawer());

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

            string skyPath = ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{arcName}.szs"));

            if (!File.Exists(skyPath))
                return;

            var skySarc = ResourceManager.FindOrLoadSARC(skyPath);

            List<ActorList> graphicsObjs = new List<ActorList>();
            ActorList skyboxList = new ActorList("Skybox");
            graphicsObjs.Add(skyboxList);

            NodeBase GraphicsObjs = new NodeBase("Graphics Objects");
            GraphicsObjs.Tag = RootNode.Tag;
            GraphicsObjs.HasCheckBox = true;
            RootNode.AddChild(GraphicsObjs);
            GraphicsObjs.Icon = IconManager.MODEL_ICON.ToString();

            LiveActor skyActor = new LiveActor(GraphicsObjs, arcName, skyPath);

            skyActor.CreateBfresRenderer(skySarc.GetModelStream(arcName));

            var bfresRender = ((BfresRender)skyActor.GetEditObj());

            bfresRender.UseDrawDistance = false;
            bfresRender.StayInFrustum = true;

            foreach (var tex in bfresRender.Textures.Values)
                tex.AlphaChannel = STChannelType.One; // disable alpha channel

            bfresRender.CanSelect = false;

            skyboxList.Add(skyActor);

            loader.AddRender(skyActor.GetDrawer());
        }

        private NodeBase CreateCategoryNode(PlacementFileEditor editor, string category, bool checkbox = true, char icon = IconManager.FOLDER_ICON)
        {
            var categoryNode = new NodeBase(category);
            categoryNode.Tag = RootNode.Tag;
            categoryNode.HasCheckBox = checkbox;
            RootNode.AddChild(categoryNode);
            categoryNode.Icon = icon.ToString();

            categoryNode.ContextMenus.Add(new MenuItemModel("Clear", () =>
            {
                for (int i = categoryNode.Children.Count - 1; i >= 0; i--)
                {
                    var layerActors = categoryNode.Children[i];

                    for (int x = layerActors.Children.Count - 1; x >= 0; x--)
                    {
                        var actorNode = layerActors.Children[x];

                        if (actorNode is EditableObjectNode editNode)
                            editor.RemoveRender(editNode.Object);
                    }

                    if (layerActors.Children.Count == 0)
                        categoryNode.Children.Remove(layerActors);
                }
            }));

            return categoryNode;
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
        }

        private LiveActor LoadActorFromPlacement(PlacementInfo placement, LayerConfig list = null)
        {
            LiveActor actor = new LiveActor(null, placement);

            actor.LoadLinks(this);

            actor.actorLayer = list;

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
            UpdateActorPlacementInfo();

            BymlArray result = new BymlArray();

            for (int i = 0; i < SCENARIO_COUNT; i++)
            {
                var scenarioByml = new BymlHash();

                foreach ((var objCategory, var layerList) in GlobalLayers)
                {
                    var scenarioPlacementList = new List<PlacementInfo>();
                    foreach (var config in layerList)
                    {
                        foreach (var placement in config.GetPlacementsInScenario(i))
                            scenarioPlacementList.Add(placement);
                    }

                    var listByml = new BymlArray();
                    int listIdx = 0;

                    foreach (var placement in scenarioPlacementList.OrderBy(e => e.objectIndex))
                    {
                        //if(placement.IsLinkDest)
                        //    ConsoleHelper.WriteWarn($"[Scenario {i}; {placement.UnitConfig.GenerateCategory}] Warning! {placement.Id} ({placement.UnitConfigName}) is marked as a link destination, but is being serialized as a root object!");

                        listByml.Add(placement.SerializeInfo(i));
                        listIdx++;
                    }

                    if (listByml.Length() > 0)
                        scenarioByml.Add(objCategory, listByml);
                }

                result.Add(scenarioByml);
            }

            return result;
        }

        private void DeserializeScenario(BymlIter rootNode, int scenarioIdx)
        {
            HashSet<string> loadedObjIds = new HashSet<string>();
            int duplicateCount = 0;

            foreach ((string objCategory, BymlIter listIter) in rootNode.As<BymlIter>())
            {
                HashSet<string> layerNames = new HashSet<string>();
                int actorCount = 0;

                foreach (BymlIter actorIter in listIter.AsArray<BymlIter>())
                {
                    PlacementId actorId = new PlacementId(actorIter);
                    bool isNeedNewId = false;

                    if(!actorId.IsValid())
                    {
                        var actorDict = actorIter.ToDictionary();
                        continue;
                    }

                    if (!GlobalLayers.TryGetValue(objCategory, out LayerList globalList))
                        GlobalLayers.Add(objCategory, globalList = new LayerList());

                    int objId = Helpers.Placement.GetPlacementObjectIndex(actorId.Id);

                    if (!loadedObjIds.Add(actorId.Id))
                    {
                        objId = ++CurrentObjectID;
                        var newId = "obj" + objId;
                        var logMsg = "Placement for actor " + actorId.UnitConfigName + " has duplicate id. Replacing with new ID. Old ID: " + actorId.Id + " New ID: " + newId;

                        StudioLogger.WriteWarning(logMsg);
                        Console.WriteLine(logMsg);

                        actorId.Id = newId;
                        isNeedNewId = true;
                        loadedObjIds.Add(newId);
                    }

                    PlacementInfo actorInfo;
                    if (globalList.IsIdInAnyLayer(actorId))
                    {
                        actorInfo = globalList.GetInfoById(actorId);

                        globalList.GetLayerByName(actorId.LayerConfigName).SetScenarioActive(scenarioIdx, true, false);
                    }
                    else
                    {
                        actorInfo = new PlacementInfo(actorIter);

                        if (isNeedNewId)
                            actorInfo.Id = actorId.Id;

                        globalList.AddObjectToLayers(actorInfo, scenarioIdx, false);
                        if (string.IsNullOrWhiteSpace(actorInfo.UnitConfig.GenerateCategory))
                            actorInfo.UnitConfig.GenerateCategory = objCategory;
                    }

                    actorInfo.UpdateLinksScenarios(actorIter, scenarioIdx);

                    actorInfo.SetScenarioActive(scenarioIdx, PlacementInfo.ScenarioFlagType.Normal);

                    layerNames.Add(actorId.LayerConfigName);

                    actorCount++;
                }
            }
        }

        private void UpdateActorPlacementInfo()
        {
            var loadedActors = GetLoadedActors(true);

            foreach (var actor in loadedActors)
                actor.UpdatePlacementInfoForSave(MapScenarioNo);
        }
    }
}