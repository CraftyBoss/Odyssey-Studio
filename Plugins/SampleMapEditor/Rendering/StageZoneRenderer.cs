
using CafeLibrary.Rendering;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using RedStarLibrary.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core.ViewModels;

namespace RedStarLibrary.Rendering
{
    public class StageZoneRenderer : GenericRenderer, IColorPickable, ITransformableObject
    {
        public class ZoneRenderEntry
        {
            public Vector3 LocalPosition;
            public Vector3 LocalRotation;
            public Vector3 LocalScale;
            public BfresRender BfresRender;

            public void ApplyLocalTransform(GLTransform parentTransform)
            {
                var worldRotQuat = parentTransform.Rotation;
                var localRotQuat = Matrix3Extension.Mat3FromEulerAnglesDeg(LocalRotation).ExtractRotation();

                var localPosRotated = Vector3.TransformPosition(LocalPosition * parentTransform.Scale, Matrix4.CreateFromQuaternion(worldRotQuat));

                BfresRender.Transform.Position = localPosRotated + parentTransform.Position;
                BfresRender.Transform.Rotation = worldRotQuat * localRotQuat;
                BfresRender.Transform.Scale = LocalScale * parentTransform.Scale;
                BfresRender.Transform.UpdateMatrix(true);
            }
        }
        public override bool UsePostEffects => true;

        public override bool IsSelected
        {
            get => base.IsSelected;
            set
            {
                base.IsSelected = value;
                foreach (var drawer in ZoneDrawers)
                    drawer.BfresRender.IsSelected = value;
            }
        }

        private BoundingNode _boundingNode;
        public override BoundingNode BoundingNode => _boundingNode;

        public List<ZoneRenderEntry> ZoneDrawers { get; private set; }

        public StageZoneRenderer(string zoneName, NodeBase parent = null) : base(parent)
        {
            ZoneDrawers = new();

            Name = zoneName;

            Transform.TransformUpdated += OnParentTransformChange;
        }

        private void OnParentTransformChange(object sender, EventArgs e)
        {
            foreach (var entry in ZoneDrawers)
                entry.ApplyLocalTransform(Transform);
        }

        public void DrawColorPicking(GLContext context)
        {
            var shader = GlobalShaders.GetShader("PICKING");
            context.ColorPicker.SetPickingColor(this, shader);

            foreach (var entry in ZoneDrawers)
                entry.BfresRender.DrawColorPicking(context);
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            foreach (var entry in ZoneDrawers)
                entry.BfresRender.DrawModel(context, pass);

            if (Toolbox.Core.Runtime.RenderBoundingBoxes)
                DrawBoundings(context);
        }

        public override void DrawColorBufferPass(GLContext context)
        {
            foreach (var entry in ZoneDrawers)
                entry.BfresRender.DrawColorBufferPass(context);
        }

        public override void DrawShadowModel(GLContext context)
        {
            foreach (var entry in ZoneDrawers)
                entry.BfresRender.DrawShadowModel(context);
        }

        public override void DrawCubeMapScene(GLContext context)
        {
            foreach (var entry in ZoneDrawers)
                entry.BfresRender.DrawCubeMapScene(context);
        }

        public override void DrawGBuffer(GLContext context)
        {
            foreach (var entry in ZoneDrawers)
                entry.BfresRender.DrawGBuffer(context);
        }

        public void AddZoneModel(Stream modelArc, string modelName, OpenTK.Vector3 pos, OpenTK.Vector3 rot, OpenTK.Vector3 scale, Dictionary<string, GenericRenderer.TextureView> textureList = null)
        {
            var renderer = new BfresRender(modelArc, modelName);
            renderer.IsRenderBoundingBox = false;
            renderer.IsSetPickingColor = false;

            if (textureList != null)
            {
                foreach (var textureName in renderer.GetUsedTextureNames())
                {
                    if (!renderer.Textures.ContainsKey(textureName) && textureList.ContainsKey(textureName))
                        renderer.Textures.Add(textureName, textureList[textureName]);
                }
            }

            ZoneDrawers.Add(new ZoneRenderEntry() { BfresRender = renderer, LocalPosition = pos, LocalRotation = rot, LocalScale = scale }) ;
        }

        public void UpdateBoundingBox()
        {
            _boundingNode = new GLFrameworkEngine.BoundingNode(new Vector3(float.MaxValue), new Vector3(float.MinValue));
            foreach (var entry in ZoneDrawers)
            {
                var renderer = entry.BfresRender;

                foreach (var model in renderer.Models)
                {
                    if (!model.IsVisible)
                        continue;

                    foreach (var mesh in model.MeshList)
                        _boundingNode.Include(mesh.BoundingNode);
                }
            }

            Transform.PropertyChanged += delegate {
                _boundingNode.UpdateTransform(Transform.TransformMatrix);
            };
        }

        public void DumpModels(string path)
        {
            string outPath = Path.Combine(path, Name);
            foreach (var drawer in ZoneDrawers)
                drawer.BfresRender.ExportModel(outPath, drawer.BfresRender.Name);
        }
        private void DrawBoundings(GLContext context)
        {
            //Go through each bounding in the current displayed mesh

            var shader = GlobalShaders.GetShader("PICKING");
            context.CurrentShader = shader;
            context.CurrentShader.SetVector4("color", new Vector4(1));

            Matrix4 transform = Transform.TransformMatrix;
            _boundingNode.UpdateTransform(transform);

            GL.LineWidth(2);

            var bnd = _boundingNode.Box;

            BoundingBoxRender.Draw(context, bnd.Min, bnd.Max);
        }
    }
}
