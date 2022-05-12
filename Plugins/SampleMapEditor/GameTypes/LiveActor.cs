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

namespace RedStarLibrary.GameTypes
{
    public enum ActorRenderMode
    {
        Basic,
        Model,
        Area
    };

    public class LiveActor
    {
        private NodeBase parent;

        public PlacementInfo placement;

        [BindGUI("Actor Name")]
        public string ActorName { get; set; }

        public bool hasArchive = false;

        public string modelPath;

        public string textureArcName;

        public Dictionary<string, string> linkedObjs;

        public EditableObject ObjectRender;
        public GLTransform Transform
        {
            get { return ObjectRender.Transform; }
        }

        public bool IsInvalidateClipping = true;

        public float ClippingDist = 10000.0f;

        private ActorRenderMode renderMode;

        public LiveActor(NodeBase parentNode, string actorName, string path)
        {
            parent = parentNode;
            placement = new PlacementInfo();

            placement.ModelName = actorName;

            linkedObjs = new Dictionary<string, string>();

            ActorName = actorName;

            modelPath = path;

            if (File.Exists(modelPath))
            {
                hasArchive = true;
            }

        }
        public LiveActor(NodeBase parentNode, PlacementInfo info)
        {
            parent = parentNode;
            placement = info;

            linkedObjs = new Dictionary<string, string>();

            ActorName = info.ModelName != null ? info.ModelName : info.UnitConifgName;

            modelPath = $"{PluginConfig.GamePath}\\ObjectData\\{ActorName}.szs";

            if (File.Exists(modelPath))
            {
                hasArchive = true;
            }
        }

        public void CreateBasicRenderer()
        {
            Console.WriteLine($"Creating Basic Render of Actor: {placement.ObjID} {placement.UnitConifgName}");
            ObjectRender = new TransformableObject(null);
            renderMode = ActorRenderMode.Basic;
            UpdateRenderer();
        }

        public void CreateBfresRenderer(Stream modelStream, Dictionary<string, GenericRenderer.TextureView> textureList = null)
        {
            Console.WriteLine($"Creating BFRES Render of Actor: {placement.ObjID} {placement.UnitConifgName}");

            ObjectRender = new BfresRender(modelStream, modelPath);

            if (textureList != null)
            {
                // TODO: Find a way to only add textures that the BFRES needs instead of every texture in the archive
                foreach (var texture in textureList)
                {
                    if (!((BfresRender)ObjectRender).Textures.ContainsKey(texture.Key))
                    {
                        ((BfresRender)ObjectRender).Textures.Add(texture.Key, texture.Value);
                    }
                }
            }

            ((BfresRender)ObjectRender).UseDrawDistance = !IsInvalidateClipping;

            if(!IsInvalidateClipping)
            {
                ((BfresRender)ObjectRender).renderDistance = ClippingDist;
                ((BfresRender)ObjectRender).renderDistanceSquared = ClippingDist * 10;
            }

            renderMode = ActorRenderMode.Model;
            UpdateRenderer();
        }

        public void CreateAreaRenderer()
        {

            Console.WriteLine($"Creating Area Render of Actor: {placement.ObjID} {placement.UnitConifgName}");

            Color areaColor = Color.Blue;

            switch (placement.ClassName)
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

            switch (placement.ModelName)
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

            renderMode = ActorRenderMode.Area;
            UpdateRenderer();
        }

        private void UpdateRenderer()
        {
            ObjectRender.ParentUINode = parent;
            ObjectRender.UINode.Header = ActorName;
            ObjectRender.UINode.Icon = IconManager.MESH_ICON.ToString();
            ObjectRender.UINode.Tag = this;
            ObjectRender.UINode.TagUI.UIDrawer += delegate
            {
                PropertyDrawer.Draw(placement.actorNode);
            };
            ObjectRender.Transform.Position = placement.translation;
            ObjectRender.Transform.Scale = placement.scale;
            ObjectRender.Transform.RotationEulerDegrees = placement.rotation;
            ObjectRender.Transform.UpdateMatrix(true);
        }
    }
}