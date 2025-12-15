using ByamlExt.Byaml;
using CafeLibrary;
using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core.ViewModels;
using CafeLibrary.Rendering;
using MapStudio.UI;
using RedStarLibrary.Rendering.Area;
using System.Drawing;
using Toolbox.Core;
using ImGuiNET;
using RedStarLibrary.Rendering;
using RedStarLibrary.MapData;
using RedStarLibrary.Extensions;
using UIFramework;
using RedStarLibrary.Helpers;
using System.Linq;
using static Toolbox.Core.Runtime;
using System.Formats.Tar;
using System.Numerics;

namespace RedStarLibrary.GameTypes
{
    public enum ActorRenderMode
    {
        Basic,
        Bfres,
        Area,
        Rail,
        Zone
    };

    public class LiveActor
    {
        private NodeBase parent = null;

        private enum RenderTarget
        {
            EditableObj,
            Rail // TODO: remove rail distinction
        }


        [BindGUI()]
        public PlacementInfo Placement { get; set; }
        /// <summary>
        /// Reference to layer actor is found in.
        /// </summary>
        public LayerConfig actorLayer;

        public bool hasArchive = false;

        public bool isPlaced = false;
        public string ArchiveName { get; set; }

        public string modelPath;

        public string textureArcName;

        public List<ActorList> linkedObjs;

        public List<ActorList> destLinkObjs;

        public GLTransform Transform
        {
            get { return objectRender.Transform; }
        }

        public ActorRenderMode RenderMode { get; private set; }

        [BindGUI("Invalidate Camera Clipping", Category = "Archive Properties")]
        public bool IsInvalidateClipping
        {
            get
            {
                return invalidateClip;
            }
            set
            {
                invalidateClip = value;

                if (objectRender is BfresRender)
                {
                    ((BfresRender)objectRender).UseDrawDistance = !value;
                }
            }
        }
        private bool invalidateClip = true;

        [BindGUI("Clipping Radius", Category = "Archive Properties")]
        public float ClippingDist { 
            get
            {
                return clipRadius;
            }
            set
            {
                clipRadius = value;
                if(objectRender is BfresRender render)
                {
                    render.renderDistance = value * 10000;
                    render.renderDistanceSquared = (value * 10) * 10000;
                }
            }
        }

        private float clipRadius = 10000.0f;

        private StageScene curStage;

        private EditableObject objectRender;

        private RenderablePath pathRender; // Why. Is. RenderablePath. Not. An. EditableObject.

        private RenderTarget renderTarget = RenderTarget.EditableObj;

        public LiveActor(NodeBase parentNode, string actorName, string path = "")
        {
            parent = parentNode;
            if(parent != null)
                curStage = (parentNode.Tag as PlacementFileEditor).CurrentMapScene;

            Placement = new PlacementInfo();

            Placement.ModelName = actorName;

            linkedObjs = new();
            destLinkObjs = new();

            ArchiveName = actorName;

            if (string.IsNullOrWhiteSpace(path))
                modelPath = ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{ArchiveName}.szs"));
            else
                modelPath = path;

            hasArchive = File.Exists(modelPath);
        }

        public LiveActor(NodeBase parentNode, PlacementInfo info, string modelPath = "")
        {
            parent = parentNode;
            if (parent != null)
                curStage = (parentNode.Tag as PlacementFileEditor).CurrentMapScene;

            Placement = info;

            linkedObjs = new();
            destLinkObjs = new();

            ArchiveName = !string.IsNullOrWhiteSpace(info.ModelName) ? info.ModelName : info.UnitConfigName;

            if (string.IsNullOrWhiteSpace(modelPath))
                this.modelPath = ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{ArchiveName}.szs"));
            else
                this.modelPath = modelPath;

            hasArchive = File.Exists(this.modelPath);
        }

        public LiveActor Clone()
        {
            LiveActor actor = new LiveActor(null, new PlacementInfo(Placement), modelPath);
            actor.actorLayer = actorLayer;
            actor.isPlaced = isPlaced;

            actor.SetParentNode(parent);

            actor.SetupRenderer();

            return actor;
        }

        public void SetParentNode(NodeBase parentNode)
        {
            parent = parentNode;

            if(parentNode.Tag is PlacementFileEditor editor)
                SetCurrentStage(editor.CurrentMapScene);
        }

        public void SetCurrentStage(StageScene stage)
        {
            curStage = stage;
        }

        public void SetupRenderer()
        {
            if (hasArchive)
            {
                TryLoadModelRenderer();
                return;
            }

            if (Placement.UnitConfig.GenerateCategory == "AreaList" || Placement.UnitConfigName.EndsWith("Area"))
            {
                ArchiveName = Placement.ClassName;
                CreateAreaRenderer();
                SetActorIcon(MapEditorIcons.AREA_BOX);
            }
            else if (Placement.ClassName == "Rail" && Placement.Id.Contains("rail")) // all rails use "rail" instead of "obj" for the id prefix
            {
                // TODO: remove classname check to be able to handle ALL types of rails (Rail, RailElectricWire, etc)
                CreateRailRenderer();
                SetActorIcon(MapEditorIcons.POINT_ICON);
            }
            else if (Placement.ClassName == "Zone")
            {
                CreateZoneRenderer();
                SetActorIcon(MapEditorIcons.OBJECT_ICON);
            }
            else
            {
                CreateBasicRenderer();
                SetActorIcon(MapEditorIcons.OBJECT_ICON);
            }
        }

        public void SetActorIcon(char icon)
        {
            if (pathRender != null)
            {
                if (pathRender is RenderablePath path)
                    path.UINode.Icon = icon.ToString();
            }
            else
            {
                objectRender.UINode.Icon = icon.ToString();
            }
        }

        public void CreateBasicRenderer()
        {
            Console.WriteLine($"Creating Basic Render of Actor: {Placement.Id} {Placement.UnitConfigName}");
            objectRender = new TransformableObject(null, 5);
            RenderMode = ActorRenderMode.Basic;
            UpdateRenderer();
        }

        public void CreateBfresRenderer(Stream modelStream, Dictionary<string, GenericRenderer.TextureView> textureList = null)
        {
            Console.WriteLine($"Creating BFRES Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            var bfresRender = new BfresRender(modelStream, ArchiveName);

            objectRender = bfresRender;

            if (textureList != null)
            {
                var usedTextures = GetUsedTextureNames();

                foreach ( var textureName in usedTextures )
                {
                    if(!bfresRender.Textures.ContainsKey(textureName))
                    {
                        if (textureList.ContainsKey(textureName))
                            bfresRender.Textures.Add(textureName, textureList[textureName]);
                        else
                            Console.WriteLine($"[{ArchiveName}] Missing Texture: {textureName}");
                    }
                }
            }

            bfresRender.UseDrawDistance = !IsInvalidateClipping;

            bfresRender.FrustumCullingCallback = () => {
                return FrustumCullActor((BfresRender)objectRender);
            };

            RenderMode = ActorRenderMode.Bfres;
            UpdateRenderer();
        }

        public void CreateAreaRenderer()
        {
            Console.WriteLine($"Creating Area Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            objectRender = new AreaRender(null, ColorUtility.ToVector4(GetAreaColor()));
            RenderMode = ActorRenderMode.Area;

            UpdateAreaShape();
            UpdateRenderer();
        }

        public void CreateRailRenderer()
        {
            // TODO: create a wrapper EditableObject for RenderablePath

            Console.WriteLine($"Creating Rail Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            pathRender = new RenderablePath();

            pathRender.UINode.Header = ArchiveName;
            pathRender.UINode.Icon = IconManager.MESH_ICON.ToString();
            pathRender.UINode.Tag = this;
            pathRender.UINode.TagUI.UIDrawer += (o, e) => PropertyDrawer.Draw(Placement.ActorParams);
            pathRender.Transform.Position = Placement.Translate;
            pathRender.Transform.Scale = Placement.Scale;
            pathRender.Transform.RotationEulerDegrees = Placement.Rotate;
            pathRender.Transform.UpdateMatrix(true);

            parent.AddChild(pathRender.UINode);

            RenderMode = ActorRenderMode.Rail;
            renderTarget = RenderTarget.Rail;

            SetupRail(pathRender, Placement);
        }

        public void CreateZoneRenderer()
        {
            var zoneStage = curStage.TryGetZone(Placement.ObjectName);

            if(zoneStage == null)
            {
                Console.WriteLine($"Zone {Placement.ObjectName} was not loaded into the editor. Cancelling Zone Renderer load.");

                CreateBasicRenderer();
                SetActorIcon(MapEditorIcons.OBJECT_ICON);
                return;
            }


            Console.WriteLine($"Creating Zone Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            var zoneRenderer = new StageZoneRenderer(Placement.ObjectName);

            objectRender = zoneRenderer;
            RenderMode = ActorRenderMode.Zone;

            foreach (var zoneInfo in zoneStage.GetLoadedPlacementInfos())
            {
                var actorModelName = !string.IsNullOrWhiteSpace(zoneInfo.ModelName) ? zoneInfo.ModelName : zoneInfo.UnitConfigName;
                var actorModelPath = ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{actorModelName}.szs"));

                if (!File.Exists(actorModelPath))
                    continue;

                var modelARC = ResourceManager.FindOrLoadSARC(actorModelPath);
                using var modelStream = modelARC.GetModelStream(actorModelName);

                if(modelStream != null)
                    zoneRenderer.AddZoneModel(modelStream, actorModelName, zoneInfo.Translate, zoneInfo.Rotate, zoneInfo.Scale, modelARC.GetTexArchive());
            }

            zoneRenderer.UpdateBoundingBox();

            UpdateRenderer();
        }

        public void ResetLinkedActors()
        {
            if(Placement.isUseLinks)
            {
                foreach (var linkList in linkedObjs)
                {
                    //foreach (var actor in linkList.Value)
                    //{
                    //    actor.isPlaced = false;
                    //    GetRenderNode().UINode.Children.Clear();
                    //}
                }
            }
        }

        public void LoadLinks(StageScene scene)
        {
            foreach(var link in Placement.Links)
            {
                var actorList = new ActorList(link);
                linkedObjs.Add(actorList);

                foreach (var placement in link)
                    actorList.Add(scene.GetOrCreateLinkActor(placement));
            }
        }

        public void PlaceLinkedObjects(PlacementFileEditor editor)
        {
            var rootNode = GetRenderNode().UINode;

            if (Placement.isUseLinks)
            {
                foreach (var linkList in linkedObjs)
                    AddLinkListToActorRender(linkList, editor);
            }
        }

        public bool TryExportModel(string outPath)
        {
            if (objectRender is BfresRender bfresRender)
                return bfresRender.ExportModel(outPath, ArchiveName);
            else if(objectRender is StageZoneRenderer zoneRender)
                zoneRender.DumpModels(outPath);
            return false;
        }

        public void LoadModel(SARC modelARC, Stream modelStream)
        {
            CreateBfresRenderer(modelStream, modelARC.GetTexArchive());
            SetActorIcon(MapEditorIcons.OBJECT_ICON);
        }

        public void SetClippingData(Stream fileStream)
        {
            BymlFileData clippingData = ByamlFile.LoadN(fileStream);

            if (((Dictionary<string, dynamic>)clippingData.RootNode).TryGetValue("Radius", out dynamic dist))
            {
                IsInvalidateClipping = false;
                ClippingDist = dist;
            }
            else if (((Dictionary<string, dynamic>)clippingData.RootNode).TryGetValue("Invalidate", out dynamic isInvalid))
            {
                IsInvalidateClipping = clippingData.RootNode["Invalidate"];
            }
        }

        public bool IsRendererSelected()
        {
            if(renderTarget == RenderTarget.EditableObj)
                return objectRender.IsSelected;
            else if(renderTarget == RenderTarget.Rail)
                return pathRender.IsSelected;
            return false;
        }

        public IDrawable GetDrawer()
        {
            if (renderTarget == RenderTarget.EditableObj)
                return objectRender;
            else
                return pathRender;
        }

        public IRenderNode GetRenderNode()
        {
            if (renderTarget == RenderTarget.EditableObj)
                return objectRender;
            else
                return pathRender;
        }

        public ITransformableObject GetTransformObj()
        {
            if (renderTarget == RenderTarget.EditableObj)
                return objectRender;
            else
                return pathRender;
        }

        public EditableObject GetEditObj() => objectRender;
        public StageZoneRenderer GetZoneRenderer() => GetEditObj() as StageZoneRenderer;
        public BfresRender GetBfresRender() => GetEditObj() as BfresRender;

        public void UpdatePlacementLinkInfo()
        {
            Placement.Links = new();

            Placement.isUseLinks = linkedObjs.Count > 0;

            foreach (var actorList in linkedObjs)
            {
                var placeList = new PlacementList(actorList.ActorListName);
                Placement.Links.Add(placeList);

                foreach (var actor in actorList)
                    placeList.Add(actor.Placement);
            }
        }

        public void UpdatePlacementInfoForSave(int scenarioIdx)
        {
            if(RenderMode == ActorRenderMode.Rail) // do manual serialization for rail actors
            {
                Placement.ActorParams["RailType"] = pathRender.InterpolationMode.ToString();
                Placement.ActorParams["IsClosed"] = pathRender.Loop;

                List<dynamic> railPoints = new();

                int idx = 0;
                foreach (var point in pathRender.PathPoints)
                {
                    PlacementInfo pointPlacement = new()
                    {
                        objectIndex = idx,
                        Id = $"{Placement.Id}/{idx++}",
                        IsLinkDest = false,
                        LayerConfigName = Placement.LayerConfigName,
                        ModelName = null,
                        Translate = point.Transform.Position,
                        Rotate = point.Transform.RotationEulerDegrees,
                        UnitConfigName = "Point"
                    };
                    pointPlacement.UnitConfig.DisplayName = pointPlacement.UnitConfigName;
                    pointPlacement.UnitConfig.ParameterConfigName = pointPlacement.UnitConfigName;
                    pointPlacement.UnitConfig.PlacementTargetFile = Placement.UnitConfig.PlacementTargetFile;
                    pointPlacement.UnitConfig.GenerateCategory = Placement.UnitConfig.GenerateCategory;

                    List<dynamic> controlPoints = new()
                    {
                        Helpers.Placement.ConvertVectorToDict(point.ControlPoint1.Transform.Position),
                        Helpers.Placement.ConvertVectorToDict(point.ControlPoint2.Transform.Position)
                    };
                    pointPlacement.ActorParams.Add("ControlPoints", controlPoints);

                    var pointContainer = pointPlacement.SerializeInfo(scenarioIdx);

                    railPoints.Add(pointContainer);
                }

                Placement.ActorParams["RailPoints"] = railPoints;
            }
        }

        private NodeBase AddLinkListToActorRender(ActorList actorList, PlacementFileEditor editor)
        {
            var rootNode = GetRenderNode().UINode;

            NodeBase linkNode = new NodeBase(actorList.ActorListName);
            linkNode.CanRename = true;
            linkNode.Tag = actorList;
            linkNode.Icon = IconManager.LINK_ICON.ToString();

            linkNode.ContextMenus.Add(new MenuItemModel("Rename", () => { linkNode.ActivateRename = true; }));
            linkNode.ContextMenus.Add(new MenuItemModel(""));

            linkNode.OnHeaderRenamed += delegate
            {
                actorList.ActorListName = linkNode.Header;
                actorList.ActorPlacements.Name = linkNode.Header;
            };

            linkNode.ContextMenus.Add(new MenuItemModel("Remove", () =>
            {
                var targetList = linkedObjs.FirstOrDefault(e => e.ActorListName == actorList.ActorListName);

                if(targetList != null)
                {
                    linkedObjs.Remove(targetList);
                    Placement.Links.Remove(targetList.ActorPlacements);

                    rootNode.Children.Remove(linkNode);
                }
            }));

            linkNode.ContextMenus.Add(new MenuItemModel("Add Actor (New)", () =>
            {
                editor.SetAddObjLinkTarget(linkNode, true);
            }));

            linkNode.ContextMenus.Add(new MenuItemModel("Add Actor (Existing)", () =>
            {

            }));

            foreach (var linkedActor in actorList)
            {
                linkNode.AddChild(linkedActor.GetRenderNode().UINode);

                if (!linkedActor.isPlaced)
                {
                    editor.AddRender(linkedActor.GetDrawer());
                    linkedActor.isPlaced = true;
                }

                linkedActor.PlaceLinkedObjects(editor);
            }

            rootNode.AddChild(linkNode);

            return linkNode;
        }

        private void AddLinkContextMenus()
        {
            objectRender.UINode.ContextMenus.Add(new MenuItemModel("Add Linked Object (New)", () =>
            {
                var objCategory = Placement.UnitConfig.GenerateCategory;
                var databaseEntry = ActorDataBase.GetObjectFromDatabase(Placement.ClassName, objCategory.Remove(objCategory.Length - 4));

                List<string> linkNames;
                if(databaseEntry != null && databaseEntry.LinkCategories.Any())
                    linkNames = databaseEntry.LinkCategories.ToList();
                else
                    linkNames = new List<string>();

                string linkCategory = "";
                bool isAddObjMenu = false;
                var editor = WorkspaceHelper.GetCurrentEditorLoader();

                // it'd be nice to be able to resize this window once the next "page" is drawing, but imgui doesnt really make this easy (no direct way to set pivot point with SetWindowPos)
                DialogHandler.Show("Add Linked Object", 400, 600, () =>
                {
                    if(isAddObjMenu)
                    {
                        editor.DrawAddObjectMenu();
                        return;
                    }

                    if (linkNames.Count > 0)
                        StudioUIHelper.DrawSelectDropdown("Link Category", ref linkCategory, linkNames);
                    ImGui.InputText("Current Category", ref linkCategory, 0x40);

                    ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 212);
                    ImGui.SetCursorPosY(ImGui.GetWindowHeight() - 35);

                    bool cancel = ImGui.Button("Cancel", new Vector2(100, 23)); ImGui.SameLine();
                    bool applied = ImGui.Button("Continue", new Vector2(100, 23));

                    if (cancel)
                        DialogHandler.ClosePopup(false);
                    if (applied)
                    {
                        NodeBase node;
                        if(!linkedObjs.Any(e=> e.ActorListName == linkCategory))
                        {
                            var newActorList = new ActorList(new PlacementList(linkCategory));
                            linkedObjs.Add(newActorList);
                            Placement.Links.Add(newActorList.ActorPlacements);

                            node = AddLinkListToActorRender(newActorList, editor);
                        }else
                        {
                            node = GetChildNodeByName(linkCategory);

                            if (node == null)
                                throw new Exception("Failed to find UI Child Node with Name: " + node);
                        }

                        editor.SetAddObjLinkTarget(node);

                        isAddObjMenu = true;
                    }

                }, editor.FinalizeAddObject);
            }));

            objectRender.UINode.ContextMenus.Add(new MenuItemModel("Add Linked Object (Existing)", () =>
            {

            }));
        }

        private List<string> GetUsedTextureNames()
        {
            if (objectRender is not BfresRender bfresRender)
                return new List<string>();

            return bfresRender.GetUsedTextureNames();
        }

        private void UpdateRenderer()
        {
            objectRender.ParentUINode = parent;

            objectRender.UINode.Header = ArchiveName;

            // failsafe if actor ends up with an empty name somehow
            if (string.IsNullOrWhiteSpace(objectRender.UINode.Header))
                objectRender.UINode.Header = Placement.ClassName;

            objectRender.UINode.Icon = IconManager.MESH_ICON.ToString();
            objectRender.UINode.Tag = this;

            objectRender.Transform.Position = Placement.Translate;
            objectRender.Transform.Scale = Placement.Scale;
            objectRender.Transform.RotationEulerDegrees = Placement.Rotate;

            objectRender.Transform.PropertyChanged +=
                (sender, args) => { ((GLTransform)sender).UpdateMatrix(); };

            objectRender.Transform.UpdateMatrix(true);

            objectRender.UINode.TagUI.UIDrawer += (o, e) => PropertyDrawer.Draw(Placement.ActorParams);

            if(!Placement.IsLinkDest) // only draw layer config if the object is a root placement
                objectRender.UINode.TagUI.UIDrawer += DrawLayerConfig;

            if (RenderMode == ActorRenderMode.Bfres || RenderMode == ActorRenderMode.Zone)
                objectRender.UINode.TagUI.UIDrawer += DrawModelProperties;
            if (RenderMode == ActorRenderMode.Area)
                objectRender.UINode.TagUI.UIDrawer += DrawAreaProperties;
            if (RenderMode == ActorRenderMode.Zone)
                objectRender.UINode.TagUI.UIDrawer += DrawZoneProperties;

            AddLinkContextMenus();

            objectRender.Transform.TransformUpdated += delegate
            {
                Placement.Translate = objectRender.Transform.Position;
                Placement.Scale = objectRender.Transform.Scale;
                Placement.Rotate = objectRender.Transform.RotationEulerDegrees;
            };

            objectRender.RemoveCallback += (obj, args) =>
            {

                if(actorLayer != null && !PlacementFileEditor.IsLoadingStage && actorLayer.LayerObjects.Remove(Placement))
                {
                    Console.WriteLine("Successfully removed Actor from Layer: " + actorLayer.LayerName);
                }

                //if (!Placement.IsLinkDest && Placement.Id != null && !EditorLoader.IsReloadingStage && LayerList.IsInfoInAnyLayer(Placement))
                //{
                //    Console.WriteLine($"Removing {Placement.Id} from Layer {Placement.LayerConfigName}");
                //    LayerList.RemoveObjectFromLayers(Placement);
                //}
            };

            objectRender.AddCallback += (obj, args) =>
            {
                if (actorLayer != null && !PlacementFileEditor.IsLoadingStage && !actorLayer.IsInfoInLayer(Placement))
                {
                    actorLayer.LayerObjects.Add(Placement);
                    Console.WriteLine("Successfully added Actor to Layer: " + actorLayer.LayerName);
                }

                //if(!Placement.IsLinkDest && Placement.Id != null && !EditorLoader.IsReloadingStage && !LayerList.IsInfoInAnyLayer(Placement, EditorLoader.MapScenarioNo))
                //{
                //    Console.WriteLine($"Adding {Placement.Id} to Layer {Placement.LayerConfigName} in Scenario {EditorLoader.MapScenarioNo}");
                //    LayerList.AddObjectToLayers(Placement, EditorLoader.MapScenarioNo, Placement.UnitConfig.GenerateCategory, true);
                //}
            };
        }

        private void DrawModelProperties(object sender, EventArgs e)
        {
            if(ImGui.CollapsingHeader("Model Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                float width = ImGui.GetWindowWidth();
                var btnSize = new System.Numerics.Vector2(width, 22);

                if (RenderMode == ActorRenderMode.Bfres && ImGui.Button("Open Model", btnSize))
                    Framework.QueueWindowFileDrop(modelPath);

                if (ImGui.Button("Export Model", btnSize))
                {
                    var dlg = new ImguiFolderDialog();

                    if (dlg.ShowDialog())
                        TryExportModel(dlg.SelectedPath);
                }

                if (RenderMode == ActorRenderMode.Bfres && ImGui.Button("Reload Model", btnSize))
                {
                    var arcName = !string.IsNullOrWhiteSpace(Placement.ModelName) ? Placement.ModelName : Placement.UnitConfigName;
                    var arcPath = ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{arcName}.szs"));

                    if(File.Exists(arcPath)) {
                        ArchiveName = arcName;
                        modelPath = arcPath;

                        SetParentNode(parent);
                        TryLoadModelRenderer(); // if a model fails to load, it wont be possible to try reloading this actors model again
                        WorkspaceHelper.AddRendererToLoader(objectRender);
                    }
                }
            }
        }

        private void DrawAreaProperties(object sender, EventArgs e)
        {
            if (ImGui.CollapsingHeader("Area Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginCombo("Area Model", Placement.ModelName))
                {
                    foreach (var model in ActorDataBase.AreaModelNames)
                    {
                        if (ImGui.Selectable(model, Placement.ModelName == model))
                        {
                            Placement.ModelName = model;
                            UpdateAreaShape();
                        }
                    }

                    ImGui.EndCombo();
                }
            }
        }

        private void DrawLayerConfig(object sender, EventArgs e)
        {
            if(ImGui.CollapsingHeader("Layer Config", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if(ImGui.BeginCombo("Actor Layer", Placement.LayerConfigName))
                {
                    foreach (var layerName in curStage.GetLoadedLayers())
                    {
                        if(ImGui.Selectable(layerName))
                            Placement.LayerConfigName = layerName;
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.BeginMenu("Active Scenarios"))
                {
                    if (ImGui.BeginTable("LayerScenarioTable", StageScene.SCENARIO_COUNT, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);

                        for (int i = 0; i < StageScene.SCENARIO_COUNT; i++)
                        {
                            ImGui.TableSetColumnIndex(i);
                            ImGui.Text((i + 1).ToString());
                        }

                        ImGui.TableNextRow();
                        for (int i = 0; i < StageScene.SCENARIO_COUNT; i++)
                        {
                            ImGui.TableSetColumnIndex(i);

                            bool isChecked = Placement.IsScenarioActive(i);
                            if (ImGui.Checkbox($"##{i}", ref isChecked))
                                Placement.SetScenarioActive(i, isChecked ? (Placement.IsLinkDest ? PlacementInfo.ScenarioFlagType.LinkOnly : PlacementInfo.ScenarioFlagType.Normal) : PlacementInfo.ScenarioFlagType.None);
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndMenu();
                }
            }
        }

        private void DrawZoneProperties(object sender, EventArgs e)
        {
            if (ImGui.CollapsingHeader("Zone Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if(ImGui.Button("Open Zone Stage"))
                {
                    Framework.QueueWindowFileDrop(ResourceManager.FindResourcePath(Path.Combine("StageData", $"{Placement.ObjectName}Map.szs")));
                }
            }
        }

        private bool FrustumCullActor(BfresRender render)
        {
            if (render.Models.Count == 0)
                return false;

            var transform = render.Transform;
            var context = GLContext.ActiveContext;

            var bounding = render.BoundingNode;
            bounding.UpdateTransform(transform.TransformMatrix);
            if (!context.Camera.InFustrum(bounding))
                return false;

            if (render.IsSelected)
                return true;

            if (render.UseDrawDistance && WorkspaceHelper.GetCurrentStageScene().IsUseClipDist)
                return context.Camera.InRange(Transform.Position, render.renderDistanceSquared);

            return true;
        }

        private void SetupRail(RenderablePath PathRender, PlacementInfo actorPlacement)
        {
            PathRender.InterpolationMode = Enum.Parse(typeof(RenderablePath.Interpolation), actorPlacement.ActorParams["RailType"]);

            PathRender.Loop = actorPlacement.ActorParams["IsClosed"];

            // TODO: rework this to use a custom PathPoint class instead of the built in one for better control
            RenderablePathPoint parentPoint = null;

            foreach (Dictionary<string, dynamic> railPoint in actorPlacement.ActorParams["RailPoints"])
            {
                var trans = Helpers.Placement.LoadVector(railPoint, "Translate");

                var point = PathRender.CreatePoint(trans);
                point.Transform.RotationEulerDegrees = Helpers.Placement.LoadVector(railPoint, "Rotate");

                if (parentPoint != null)
                    parentPoint.AddChild(point);

                // rail points always have two control points, regardless of if they're Linear or Bezier
                point.ControlPoint1.Transform.Position = Helpers.Placement.LoadVector(railPoint["ControlPoints"][0]);
                point.ControlPoint2.Transform.Position = Helpers.Placement.LoadVector(railPoint["ControlPoints"][1]);

                point.UpdateMatrices();

                PathRender.AddPoint(point);

                parentPoint = point;
            }
        }

        internal void TryLoadModelRenderer()
        {
            if(objectRender != null)
                WorkspaceHelper.RemoveRendererFromActiveScene(objectRender);

            var modelARC = ResourceManager.FindOrLoadSARC(modelPath);
            var modelName = Path.GetFileNameWithoutExtension(modelPath);

            using var modelStream = modelARC.GetModelStream(modelName);

            if (modelStream != null)
            {
                LoadModel(modelARC, modelStream);

                var fileStream = modelARC.GetInitFileStream("InitClipping");

                if (fileStream != null)
                    SetClippingData(fileStream);

                SetActorIcon(Rendering.MapEditorIcons.OBJECT_ICON);
            }
            else
            {
                CreateBasicRenderer();
                SetActorIcon(Rendering.MapEditorIcons.OBJECT_ICON);
            }
        }

        private void UpdateAreaShape()
        {
            if (RenderMode != ActorRenderMode.Area)
                return;
            var areaShape = AreaRender.AreaType.CubeBase;

            if(!string.IsNullOrWhiteSpace(Placement.ModelName) && Placement.ModelName.StartsWith("Area"))
            {
                // all area models start with "AreaX", so remove Area from string before parsing to enum type
                if (!Enum.TryParse(Placement.ModelName.Substring(4), out areaShape))
                    StudioLogger.WriteError("Invalid ModelName for Area! Name: " + Placement.ObjectName);
            }else
            {
                Placement.ModelName = "Area" + areaShape.ToString();
            }

            ((AreaRender)objectRender).AreaShape = areaShape;
        }

        private Color GetAreaColor()
        {
            Color areaColor = Color.Blue;

            switch (Placement.ClassName)
            {
                case string a when a == "DeathArea":
                    areaColor = Color.Red;
                    break;
                case string a when a == "CameraArea":
                    areaColor = Color.Green;
                    break;
                default:
                    break;
            }

            return areaColor;
        }

        private NodeBase GetChildNodeByName(string name)
        {
            var rootNode = GetRenderNode().UINode;

            return rootNode.Children.FirstOrDefault(e => e.Header == name);
        }
    }
}