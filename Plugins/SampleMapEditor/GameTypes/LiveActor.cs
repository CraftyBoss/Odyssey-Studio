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

        private ActorRenderMode renderMode;

        public LiveActor(NodeBase parentNode, string actorName, string path)
        {
            parent = parentNode;
            Placement = new PlacementInfo();

            Placement.ModelName = actorName;

            linkedObjs = new Dictionary<string, List<LiveActor>>();
            destLinkObjs = new Dictionary<string, List<LiveActor>>();

            ArchiveName = actorName;

            modelPath = path;

            if (File.Exists(modelPath))
            {
                hasArchive = true;
            }
        }
        public LiveActor(NodeBase parentNode, PlacementInfo info)
        {
            parent = parentNode;
            Placement = info;

            linkedObjs = new Dictionary<string, List<LiveActor>>();
            destLinkObjs = new Dictionary<string, List<LiveActor>>();

            ArchiveName = info.ModelName != null ? info.ModelName : info.UnitConfigName;

            modelPath = ResourceManager.FindResourcePath($"ObjectData\\{ArchiveName}.szs");

            if (File.Exists(modelPath))
            {
                hasArchive = true;
            }

        }

        public void SetParentNode(NodeBase parentNode)
        {
            parent = parentNode;

            if (renderMode == ActorRenderMode.EditableObj)
                ObjectRender.ParentUINode = parent;
            else if (renderMode == ActorRenderMode.Drawable)
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
            renderMode = ActorRenderMode.EditableObj;
            UpdateRenderer();
        }

        public void CreateBfresRenderer(Stream modelStream, Dictionary<string, GenericRenderer.TextureView> textureList = null)
        {
            Console.WriteLine($"Creating BFRES Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            ObjectRender = new BfresRender(modelStream, modelPath);

            var bfresRender = (BfresRender)ObjectRender;

            if (textureList != null)
            {
                // TODO: Find a way to only add textures that the BFRES needs instead of every texture in the archive
                foreach (var texture in textureList)
                {
                    if (!bfresRender.Textures.ContainsKey(texture.Key))
                    {
                        bfresRender.Textures.Add(texture.Key, texture.Value);
                    }
                }
            }

            bfresRender.UseDrawDistance = !IsInvalidateClipping;

            bfresRender.FrustumCullingCallback = () => {
                return FrustumCullActor((BfresRender)ObjectRender);
            };

            renderMode = ActorRenderMode.EditableObj;
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

            renderMode = ActorRenderMode.EditableObj;
            UpdateRenderer();
        }

        public void CreateRailRenderer()
        {
            Console.WriteLine($"Creating Rail Render of Actor: {Placement.Id} {Placement.UnitConfigName}");

            var rail = new RenderablePath();

            rail.UINode.Header = ArchiveName;
            rail.UINode.Icon = IconManager.MESH_ICON.ToString();
            rail.UINode.Tag = this;
            if (Placement.ActorParams.Count > 0)
            {
                rail.UINode.TagUI.UIDrawer += delegate
                {
                    PropertyDrawer.Draw(Placement.ActorParams);
                };
            }
            rail.Transform.Position = Placement.Translate;
            rail.Transform.Scale = Placement.Scale;
            rail.Transform.RotationEulerDegrees = Placement.Rotate;
            rail.Transform.UpdateMatrix(true);

            renderMode = ActorRenderMode.Drawable;

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

                        if (renderMode == ActorRenderMode.EditableObj)
                            ObjectRender.UINode.Children.Clear();
                        else if (renderMode == ActorRenderMode.Drawable)
                            ((RenderablePath)ObjectDrawer).UINode.Children.Clear();
                    }
                }
            }
        }

        public void PlaceLinkedObjects(EditorLoader loader)
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

                if (renderMode == ActorRenderMode.EditableObj)
                    ObjectRender.UINode.AddChild(rootLinkNode);
                else if (renderMode == ActorRenderMode.Drawable)
                    ((RenderablePath)ObjectDrawer).UINode.AddChild(rootLinkNode);
                    
            }
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
            ObjectRender.Transform.UpdateMatrix(true);

            if (Placement.ActorParams.Count > 0)
            {
                ObjectRender.UINode.TagUI.UIDrawer += delegate
                {
                    PropertyDrawer.Draw(Placement.ActorParams);
                };
            }

            ObjectRender.Transform.TransformUpdated += delegate
            {
                Placement.Translate = ObjectRender.Transform.Position;
                Placement.Scale = ObjectRender.Transform.Scale;
                Placement.Rotate = ObjectRender.Transform.RotationEulerDegrees;
            };

            ObjectRender.RemoveCallback += (obj, args) =>
            {

                if(actorLayer != null && !EditorLoader.IsLoadingStage && actorLayer.LayerObjects.Remove(Placement))
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
                if (actorLayer != null && !EditorLoader.IsLoadingStage && !actorLayer.IsInfoInLayer(Placement))
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

            if (render.UseDrawDistance)
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
    }
}