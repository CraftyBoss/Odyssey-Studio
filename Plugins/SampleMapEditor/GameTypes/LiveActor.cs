﻿using ByamlExt.Byaml;
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
        EditableObj,
        Drawable
    };

    public class LiveActor
    {
        private NodeBase parent;


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

        public Dictionary<string, List<LiveActor>> linkedObjs;

        public Dictionary<string, List<LiveActor>> destLinkObjs;

        public EditableObject ObjectRender;

        public IDrawable ObjectDrawer;
        public GLTransform Transform
        {
            get { return ObjectRender.Transform; }
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

                if (ObjectRender is BfresRender)
                {
                    ((BfresRender)ObjectRender).UseDrawDistance = !value;
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
                if(ObjectRender is BfresRender render)
                {
                    render.renderDistance = value * 10000;
                    render.renderDistanceSquared = (value * 10) * 10000;
                }
            }
        }

        private float clipRadius = 10000.0f;

        private StageScene curStage;

        public LiveActor(NodeBase parentNode, string actorName, string path)
        {
            parent = parentNode;
            if(parent != null)
                curStage = (parentNode.Tag as PlacementFileEditor).CurrentMapScene;

            Placement = new PlacementInfo();

            Placement.ModelName = actorName;

            linkedObjs = new Dictionary<string, List<LiveActor>>();
            destLinkObjs = new Dictionary<string, List<LiveActor>>();

            ArchiveName = actorName;

            modelPath = path;

            hasArchive = File.Exists(modelPath);
        }

        public LiveActor(NodeBase parentNode, PlacementInfo info)
        {
            parent = parentNode;
            if (parent != null)
                curStage = (parentNode.Tag as PlacementFileEditor).CurrentMapScene;

            Placement = info;

            linkedObjs = new Dictionary<string, List<LiveActor>>();
            destLinkObjs = new Dictionary<string, List<LiveActor>>();

            ArchiveName = !string.IsNullOrEmpty(info.ModelName) ? info.ModelName : info.UnitConfigName;

            modelPath = ResourceManager.FindResourcePath($"ObjectData\\{ArchiveName}.szs");

            hasArchive = File.Exists(modelPath);
        }

        public void SetParentNode(NodeBase parentNode)
        {
            parent = parentNode;
            curStage = (parentNode.Tag as PlacementFileEditor).CurrentMapScene;

            if (RenderMode == ActorRenderMode.EditableObj)
                ObjectRender.ParentUINode = parent;
            else if (RenderMode == ActorRenderMode.Drawable)
                parent.AddChild(((RenderablePath)ObjectDrawer).UINode);
        }

        public void SetActorIcon(char icon)
        {
            if (ObjectDrawer != null)
            {
                if (ObjectDrawer is RenderablePath path)
                    path.UINode.Icon = icon.ToString();
            }
            else
            {
                ObjectRender.UINode.Icon = icon.ToString();
            }
        }

        public void CreateBasicRenderer()
        {
            Console.WriteLine($"Creating Basic Render of Actor: {Placement.Id} {Placement.UnitConfigName}");
            ObjectRender = new TransformableObject(null);
            RenderMode = ActorRenderMode.EditableObj;
            UpdateRenderer();
        }

        public void CreateBfresRenderer(Stream modelStream, Dictionary<string, GenericRenderer.TextureView> textureList = null)
        {
            Console.WriteLine($"Creating BFRES Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            var bfresRender = new BfresRender(modelStream, ArchiveName);

            ObjectRender = bfresRender;

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
                return FrustumCullActor((BfresRender)ObjectRender);
            };

            RenderMode = ActorRenderMode.EditableObj;
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

            ObjectRender = new AreaRender(null, ColorUtility.ToVector4(areaColor));

            switch (Placement.ModelName)
            {
                case string a when a.Contains("Cube"):
                    ((AreaRender)ObjectRender).AreaShape = AreaRender.AreaType.CubeBase;
                    break;
                case string b when b.Contains("Sphere"):
                    ((AreaRender)ObjectRender).AreaShape = AreaRender.AreaType.Sphere;
                    break;
                case string c when c.Contains("Cylinder"):
                    ((AreaRender)ObjectRender).AreaShape = AreaRender.AreaType.CylinderBase;
                    break;
                default:
                    ((AreaRender)ObjectRender).AreaShape = AreaRender.AreaType.CubeBase;
                    break;
            }

            RenderMode = ActorRenderMode.EditableObj;
            UpdateRenderer();
        }

        public void CreateRailRenderer()
        {
            Console.WriteLine($"Creating Rail Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            var rail = new RenderablePath();

            rail.UINode.Header = ArchiveName;
            rail.UINode.Icon = IconManager.MESH_ICON.ToString();
            rail.UINode.Tag = this;
            ObjectRender.UINode.TagUI.UIDrawer += (o, e) => PropertyDrawer.Draw(Placement.ActorParams);
            rail.Transform.Position = Placement.Translate;
            rail.Transform.Scale = Placement.Scale;
            rail.Transform.RotationEulerDegrees = Placement.Rotate;
            rail.Transform.UpdateMatrix(true);

            RenderMode = ActorRenderMode.Drawable;

            SetupRail(rail, Placement);

            ObjectDrawer = rail;
        }

        public void ResetLinkedActors()
        {
            if(Placement.isUseLinks)
            {
                foreach (var linkList in linkedObjs)
                {
                    foreach (var actor in linkList.Value)
                    {
                        actor.isPlaced = false;

                        if (RenderMode == ActorRenderMode.EditableObj)
                            ObjectRender.UINode.Children.Clear();
                        else if (RenderMode == ActorRenderMode.Drawable)
                            ((RenderablePath)ObjectDrawer).UINode.Children.Clear();
                    }
                }
            }
        }

        public void PlaceLinkedObjects(PlacementFileEditor loader)
        {

            if(Placement.isUseLinks)
            {
                NodeBase rootLinkNode = new NodeBase("Linked Objects");
                rootLinkNode.Icon = IconManager.LINK_ICON.ToString();

                foreach (var linkList in linkedObjs)
                {
                    NodeBase linkNode = new NodeBase(linkList.Key);

                    foreach (var linkedActor in linkList.Value)
                    {
                        if(linkedActor.ObjectDrawer != null)
                        {
                            if (linkedActor.ObjectDrawer is RenderablePath path)
                                linkNode.AddChild(path.UINode);
                        }
                        else
                        {
                            linkNode.AddChild(linkedActor.ObjectRender.UINode);
                        }

                        if (!linkedActor.isPlaced)
                        {
                            if (linkedActor.ObjectDrawer != null)
                                loader.AddRender(linkedActor.ObjectDrawer);
                            else
                                loader.AddRender(linkedActor.ObjectRender);
                            
                            linkedActor.isPlaced = true;
                        }

                        linkedActor.PlaceLinkedObjects(loader);

                    }

                    rootLinkNode.AddChild(linkNode);

                }

                if (RenderMode == ActorRenderMode.EditableObj)
                    ObjectRender.UINode.AddChild(rootLinkNode);
                else if (RenderMode == ActorRenderMode.Drawable)
                    ((RenderablePath)ObjectDrawer).UINode.AddChild(rootLinkNode);
                    
            }
        }

        public bool TryExportModel(string outPath)
        {
            if (ObjectRender is BfresRender bfresRender)
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
                    arcTex.OriginalSource.Export($"{outPath}\\{texName}.png", new TextureExportSettings());

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

        private List<string> GetUsedTextureNames()
        {
            if (ObjectRender is not BfresRender bfresRender)
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
            ObjectRender.ParentUINode = parent;
            ObjectRender.UINode.Header = ArchiveName;
            ObjectRender.UINode.Icon = IconManager.MESH_ICON.ToString();
            ObjectRender.UINode.Tag = this;

            ObjectRender.Transform.Position = Placement.Translate;
            ObjectRender.Transform.Scale = Placement.Scale;
            ObjectRender.Transform.RotationEulerDegrees = Placement.Rotate;

            ObjectRender.Transform.PropertyChanged +=
                (sender, args) => { ((GLTransform)sender).UpdateMatrix(); };

            ObjectRender.Transform.UpdateMatrix(true);

            ObjectRender.UINode.TagUI.UIDrawer += (o, e) => PropertyDrawer.Draw(Placement.ActorParams);
            ObjectRender.UINode.TagUI.UIDrawer += DrawLayerConfig;

            if (ObjectRender is BfresRender)
                ObjectRender.UINode.TagUI.UIDrawer += DrawModelProperties;

            ObjectRender.Transform.TransformUpdated += delegate
            {
                Placement.Translate = ObjectRender.Transform.Position;
                Placement.Scale = ObjectRender.Transform.Scale;
                Placement.Rotate = ObjectRender.Transform.RotationEulerDegrees;
            };

            ObjectRender.RemoveCallback += (obj, args) =>
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

            ObjectRender.AddCallback += (obj, args) =>
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

                if(ImGui.Button("Reload Model", btnSize))
                {
                    var arcName = !string.IsNullOrEmpty(Placement.ModelName) ? Placement.ModelName : Placement.UnitConfigName;
                    var arcPath = ResourceManager.FindResourcePath($"ObjectData\\{arcName}.szs");

                    if(File.Exists(arcPath)) {
                        ArchiveName = arcName;
                        modelPath = arcPath;

                        TryLoadModelRenderer();
                        SetParentNode(parent);
                        WorkspaceHelper.AddRendererToLoader(ObjectRender);
                    }
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

            foreach (Dictionary<string, dynamic> railPoint in actorPlacement.ActorParams["RailPoints"])
            {

                var trans = Helpers.Placement.LoadVector(railPoint, "Translate");

                var point = PathRender.CreatePoint(trans);

                if (PathRender.InterpolationMode == RenderablePath.Interpolation.Bezier)
                {
                    point.ControlPoint1.Transform.Position = Helpers.Placement.LoadVector(railPoint["ControlPoints"][0]);
                    point.ControlPoint2.Transform.Position = Helpers.Placement.LoadVector(railPoint["ControlPoints"][1]);
                }

                point.UpdateMatrices();

                PathRender.AddPoint(point);
            }
        }

        internal void TryLoadModelRenderer()
        {
            if(ObjectRender != null)
                WorkspaceHelper.RemoveRendererFromActiveScene(ObjectRender);

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
    }
}