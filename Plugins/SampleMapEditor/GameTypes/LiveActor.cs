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
using SampleMapEditor.Rendering.Area;
using System.Drawing;
using Toolbox.Core;

namespace SampleMapEditor.GameTypes
{
    public enum ActorRenderMode
    {
        Basic,
        Model,
        Area
    };

    public class LiveActor : GenericRenderer, IColorPickable, ITransformableObject
    {
        public LiveActor(NodeBase parentNode, PlacementInfo info) : base(parentNode)
        {
            placement = info;

            linkedObjs = new Dictionary<string, string>();

            actorName = info.ModelName != null ? info.ModelName : info.UnitConifgName;

            modelPath = $"{PluginConfig.GamePath}\\ObjectData\\{actorName}.szs";

            if (File.Exists(modelPath))
            {
                hasArchive = true;
            }

            UINode.Header = placement.ClassName;
            UINode.Icon = IconManager.MESH_ICON.ToString();
            Transform.Position = placement.translation;
            Transform.Scale = placement.scale;
            Transform.RotationEulerDegrees = placement.rotation;

        }

        public PlacementInfo placement;

        [BindGUI("Actor Name")]
        public string actorName;

        public bool hasArchive = false;

        public string modelPath;

        public string textureArcName;

        public Dictionary<string, string> linkedObjs;

        private ActorRenderMode renderMode;

        private BfresRender BFRESRenderer;

        private TransformableObject BasicRenderer;

        private AreaRender AreaRenderer;

        // BFRES Render Overrides
        public override bool UsePostEffects
        {
            get { return renderMode == ActorRenderMode.Model; }
        }

        public override bool IsSelected 
        {
            get => base.IsSelected;
            set
            {

                base.IsSelected = value;

                switch (renderMode)
                {
                    case ActorRenderMode.Basic:

                        BasicRenderer.IsSelected = value;

                        break;
                    case ActorRenderMode.Model:
                        BFRESRenderer.IsSelected = value;

                        foreach (var model in BFRESRenderer.Models)
                        {
                            foreach (BfresMeshRender mesh in model.MeshList)
                            {
                                mesh.IsSelected = value;
                            }
                        }

                        break;
                    case ActorRenderMode.Area:
                        AreaRenderer.IsSelected = value;
                        break;
                }
            }
        }

        public override bool InFrustum {
            get { return renderMode == ActorRenderMode.Model ? BFRESRenderer.InFrustum : false; }
            set { if (renderMode == ActorRenderMode.Model) BFRESRenderer.InFrustum = value; }
        }

        public override BoundingNode BoundingNode
        {
            get { return renderMode == ActorRenderMode.Model ? BFRESRenderer.BoundingNode : base.BoundingNode; }
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            switch (renderMode)
            {
                case ActorRenderMode.Basic:
                    BasicRenderer.Transform = Transform;
                    BasicRenderer.DrawModel(context, pass);
                    break;
                case ActorRenderMode.Model:
                    BFRESRenderer.Transform = Transform;
                    BFRESRenderer.DrawModel(context, pass);
                    break;
                case ActorRenderMode.Area:
                    AreaRenderer.Transform = Transform;
                    AreaRenderer.DrawModel(context, pass);
                    break;
                default:
                    break;
            }
        }

        public void CreateBasicRenderer()
        {
            Console.WriteLine($"Creating Basic Render of Actor: {placement.ObjID} {placement.UnitConifgName}");
            BasicRenderer = new TransformableObject(null);
            renderMode = ActorRenderMode.Basic;
        }

        public void CreateBfresRenderer(Stream modelStream, Dictionary<string, GenericRenderer.TextureView> textureList = null)
        {
            Console.WriteLine($"Creating BFRES Render of Actor: {placement.ObjID} {placement.UnitConifgName}");

            BFRESRenderer = new BfresRender(modelStream, modelPath);

            if (textureList != null)
            {
                // TODO: Find a way to only add textures that the BFRES needs instead of every texture in the archive
                foreach (var texture in textureList)
                {
                    if (!BFRESRenderer.Textures.ContainsKey(texture.Key))
                    {
                        BFRESRenderer.Textures.Add(texture.Key, texture.Value);
                    }
                }
            }

            renderMode = ActorRenderMode.Model;
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

            AreaRenderer = new AreaRender(null, ColorUtility.ToVector4(areaColor));

            switch (placement.ModelName)
            {
                case string a when a.Contains("Cube"):
                    AreaRenderer.AreaShape = AreaRender.AreaType.CubeBase;
                    break;
                case string b when b.Contains("Sphere"):
                    AreaRenderer.AreaShape = AreaRender.AreaType.Sphere;
                    break;
                case string c when c.Contains("Cylinder"):
                    AreaRenderer.AreaShape = AreaRender.AreaType.CylinderBase;
                    break;
                default:
                    AreaRenderer.AreaShape = AreaRender.AreaType.CubeBase;
                    break;
            }

            renderMode = ActorRenderMode.Area;
        }

        // BFRES Render Overrides

        public override bool IsInsideFrustum(GLContext context) { return renderMode == ActorRenderMode.Model ? BFRESRenderer.IsInsideFrustum(context) : true; }

        public override void ResetAnimations() { if (renderMode == ActorRenderMode.Model) BFRESRenderer.ResetAnimations(); }

        public override void DrawColorBufferPass(GLContext control) { if (renderMode == ActorRenderMode.Model) BFRESRenderer.DrawColorBufferPass(control); }

        public override void DrawShadowModel(GLContext control) { if (renderMode == ActorRenderMode.Model) BFRESRenderer.DrawShadowModel(control); }

        public override void DrawCubeMapScene(GLContext control) { if (renderMode == ActorRenderMode.Model) BFRESRenderer.DrawCubeMapScene(control); }

        public override void DrawGBuffer(GLContext control) { if (renderMode == ActorRenderMode.Model) BFRESRenderer.DrawGBuffer(control); }

        public void DrawColorPicking(GLContext control) { if (renderMode == ActorRenderMode.Model) BFRESRenderer.DrawColorPicking(control); }

    }
}
