using ByamlExt.Byaml;
using CafeLibrary;
using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core.ViewModels;
using CafeLibrary.Rendering;
using MapStudio.UI;
using RedStarLibrary.Rendering.Area;
using System.Drawing;
using Toolbox.Core;
using ImGuiNET;
using RedStarLibrary.Rendering;
using RedStarLibrary.MapData;
using BfresLibrary;
using CafeLibrary.ModelConversion;
using RedStarLibrary.Extensions;
using UIFramework;
using Newtonsoft.Json.Linq;
using RedStarLibrary.Helpers;
using System.IO.Pipes;

namespace RedStarLibrary.GameTypes
{
    public enum ActorRenderMode
    {
        Basic,
        Bfres,
        Area,
        Rail
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

            if (string.IsNullOrEmpty(path))
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

            ArchiveName = !string.IsNullOrEmpty(info.ModelName) ? info.ModelName : info.UnitConfigName;

            if (string.IsNullOrEmpty(modelPath))
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

            actor.SetupRenderer();

            actor.SetParentNode(parent);

            return actor;
        }

        public void SetParentNode(NodeBase parentNode)
        {
            parent = parentNode;
            curStage = (parentNode.Tag as PlacementFileEditor).CurrentMapScene;

            if (renderTarget == RenderTarget.EditableObj)
                objectRender.ParentUINode = parent;
            else if (renderTarget == RenderTarget.Rail)
                parent.AddChild(pathRender.UINode);
        }

        public void SetupRenderer()
        {
            if (hasArchive)
            {
                TryLoadModelRenderer();
                return;
            }

            if (Placement.UnitConfig.GenerateCategory == "AreaList")
            {
                ArchiveName = Placement.ClassName;
                CreateAreaRenderer();
                SetActorIcon(MapEditorIcons.AREA_BOX);
            }
            else if (Placement.Id.Contains("rail")) // all rails use "rail" instead of "obj" for the id prefix
            {
                CreateRailRenderer();
                SetActorIcon(MapEditorIcons.POINT_ICON);
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

            objectRender = new AreaRender(null, ColorUtility.ToVector4(areaColor));
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

            RenderMode = ActorRenderMode.Rail;
            renderTarget = RenderTarget.Rail;

            SetupRail(pathRender, Placement);
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
                ActorList linkList = new(link.Key);
                linkedObjs.Add(linkList);

                foreach (var placementLink in link.Value)
                    linkList.Add(scene.GetOrCreateLinkActor(placementLink));
            }
        }

        public void PlaceLinkedObjects(PlacementFileEditor loader)
        {
            if(Placement.isUseLinks)
            {
                NodeBase rootLinkNode = new NodeBase("Links");
                rootLinkNode.Icon = IconManager.LINK_ICON.ToString();

                foreach (var linkList in linkedObjs)
                {
                    NodeBase linkNode = new NodeBase(linkList.ActorListName);

                    foreach (var linkedActor in linkList)
                    {
                        linkNode.AddChild(linkedActor.GetRenderNode().UINode);

                        if (!linkedActor.isPlaced)
                        {
                            loader.AddRender(linkedActor.GetDrawer());
                            linkedActor.isPlaced = true;
                        }

                        linkedActor.PlaceLinkedObjects(loader);
                    }

                    rootLinkNode.AddChild(linkNode);
                }

                GetRenderNode().UINode.AddChild(rootLinkNode);
            }
        }

        public bool TryExportModel(string outPath)
        {
            if (objectRender is BfresRender bfresRender)
            {
                outPath = Path.Combine(outPath, ArchiveName);
                if (!Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);
                else // dont bother re-dumping if the model folder is already present
                    return true;

                var resFile = bfresRender.ResFile;

                if (resFile == null)
                    throw new NullReferenceException();

                foreach (var model in resFile.Models)
                {
                    var modelPath = Path.Combine(outPath, model.Key + ".dae");

                    var scene = BfresModelExporter.FromGeneric(resFile, model.Value);
                    IONET.IOManager.ExportScene(scene, modelPath, new IONET.ExportSettings() { });
                }

                foreach ((var texName, var arcTex) in bfresRender.Textures)
                    arcTex.OriginalSource.Export(Path.Combine(outPath, $"{texName}.png"), new TextureExportSettings());

                return true;
            }
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

        private List<string> GetUsedTextureNames()
        {
            if (objectRender is not BfresRender bfresRender)
                return new List<string>();

            List<string> result = new List<string>();

            foreach (BfresModelRender model in bfresRender.Models)
            {
                foreach (BfresMeshRender mesh in model.Meshes)
                {
                    if (mesh.MaterialAsset is not SMORenderer matAsset)
                        continue;

                    foreach (var texMap in matAsset.Material.TextureMaps)
                    {
                        if(!result.Contains(texMap.Name))
                            result.Add(texMap.Name);
                    }
                }
            }

            return result;
        }

        private void UpdateRenderer()
        {
            objectRender.ParentUINode = parent;
            objectRender.UINode.Header = ArchiveName;
            objectRender.UINode.Icon = IconManager.MESH_ICON.ToString();
            objectRender.UINode.Tag = this;

            objectRender.Transform.Position = Placement.Translate;
            objectRender.Transform.Scale = Placement.Scale;
            objectRender.Transform.RotationEulerDegrees = Placement.Rotate;

            objectRender.Transform.PropertyChanged +=
                (sender, args) => { ((GLTransform)sender).UpdateMatrix(); };

            objectRender.Transform.UpdateMatrix(true);

            objectRender.UINode.TagUI.UIDrawer += (o, e) => PropertyDrawer.Draw(Placement.ActorParams);
            objectRender.UINode.TagUI.UIDrawer += DrawLayerConfig;

            if (RenderMode == ActorRenderMode.Bfres)
                objectRender.UINode.TagUI.UIDrawer += DrawModelProperties;
            else if (RenderMode == ActorRenderMode.Area)
                objectRender.UINode.TagUI.UIDrawer += DrawAreaProperties;

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

                if (ImGui.Button("Open Model", btnSize))
                    Framework.QueueWindowFileDrop(modelPath);

                if (ImGui.Button("Export Model", btnSize))
                {
                    var dlg = new ImguiFolderDialog();

                    if (dlg.ShowDialog())
                        TryExportModel(dlg.SelectedPath);
                }

                if (ImGui.Button("Reload Model", btnSize))
                {
                    var arcName = !string.IsNullOrEmpty(Placement.ModelName) ? Placement.ModelName : Placement.UnitConfigName;
                    var arcPath = ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{arcName}.szs"));

                    if(File.Exists(arcPath)) {
                        ArchiveName = arcName;
                        modelPath = arcPath;

                        TryLoadModelRenderer(); // if a model fails to load, it wont be possible to try reloading this actors model again
                        SetParentNode(parent);
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
                                Placement.SetScenarioActive(i, isChecked);
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndMenu();
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

            RenderablePathPoint parentPoint = null;

            foreach (Dictionary<string, dynamic> railPoint in actorPlacement.ActorParams["RailPoints"])
            {

                var trans = Helpers.Placement.LoadVector(railPoint, "Translate");

                var point = PathRender.CreatePoint(trans);

                if(parentPoint != null)
                    parentPoint.AddChild(point);

                if (PathRender.InterpolationMode == RenderablePath.Interpolation.Bezier)
                {
                    point.ControlPoint1.Transform.Position = Helpers.Placement.LoadVector(railPoint["ControlPoints"][0]);
                    point.ControlPoint2.Transform.Position = Helpers.Placement.LoadVector(railPoint["ControlPoints"][1]);
                }

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

                CreateBfresRenderer(modelStream, modelARC.GetTexArchive());

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

            if(!string.IsNullOrWhiteSpace(Placement.ModelName))
            {
                // all area models start with "AreaX", so remove Area from string before parsing to enum type
                if (!Enum.TryParse(Placement.ModelName.Substring(4), out areaShape))
                    throw new InvalidOperationException("Invalid ModelName for Area! Name: " + Placement.ObjectName);
            }

            ((AreaRender)objectRender).AreaShape = areaShape;
        }
    }
}