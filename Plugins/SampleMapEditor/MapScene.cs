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
using SampleMapEditor.GameTypes;
using SampleMapEditor.Rendering.Area;

namespace SampleMapEditor
{
    internal class MapScene
    {
        public void Setup(EditorLoader loader)
        {
            //Prepare a collision caster for snapping objects onto
            SetupSceneCollision();
            //Add some objects to the scene
            SetupObjects(loader);
        }

        private void CreateTransformObject(EditorLoader loader, NodeBase list, LiveActor actor)
        {
            //These are default transform cubes
            //You give it the folder you want to parent in the tree or make it null to not be present.
            Console.WriteLine($"Placing Transform Object: {actor.placement.ModelName}");
            TransformableObject obj = new TransformableObject(list);
            //Name
            obj.UINode.Header = actor.actorName;
            obj.UINode.Icon = IconManager.MESH_ICON.ToString();
            //Give it a transform in the scene
            obj.Transform.Position = actor.placement.translation;
            obj.Transform.Scale = actor.placement.scale;
            obj.Transform.RotationEulerDegrees = actor.placement.rotation;
            //You need to force update it. This is not updated per frame to save on performance
            obj.Transform.UpdateMatrix(true);
            //Lastly add your object to the scene
            loader.AddRender(obj);
        }

        private void CreateBfresObject(EditorLoader loader, NodeBase list, LiveActor actor, Stream modelStream, Dictionary<string, GenericRenderer.TextureView> textureList = null)
        {
            Console.WriteLine($"Placing BFRES Object: {actor.actorName}");

            BfresRender bfresObj = new BfresRender(modelStream, actor.modelPath);

            if(textureList != null)
            {
                // TODO: Find a way to only add textures that the BFRES needs instead of every texture in the archive
                foreach (var texture in textureList)
                {
                    if(!bfresObj.Textures.ContainsKey(texture.Key))
                    {
                        bfresObj.Textures.Add(texture.Key, texture.Value);
                    }
                }
            }

            bfresObj.UINode.Header = actor.actorName;
            bfresObj.UINode.Icon = IconManager.MESH_ICON.ToString();
            bfresObj.Transform.Position = actor.placement.translation;
            bfresObj.Transform.Scale = actor.placement.scale;
            bfresObj.Transform.RotationEulerDegrees = actor.placement.rotation;
            bfresObj.Transform.UpdateMatrix(true);

            list.AddChild(bfresObj.UINode);

            loader.AddRender(bfresObj);
        }

        private void CreateAreaObject(EditorLoader loader, NodeBase list, LiveActor actor)
        {
            Color areaColor = Color.Blue;

            switch (actor.placement.ClassName)
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

            AreaRender areaObj = new AreaRender(list, ColorUtility.ToVector4(areaColor));

            areaObj.UINode.Header = actor.placement.ClassName;
            areaObj.UINode.Icon = IconManager.MESH_ICON.ToString();
            areaObj.Transform.Position = actor.placement.translation;
            areaObj.Transform.Scale = actor.placement.scale;
            areaObj.Transform.RotationEulerDegrees = actor.placement.rotation;

            switch (actor.placement.ModelName)
            {
                case string a when a.Contains("Cube"):
                    areaObj.AreaShape = AreaRender.AreaType.CubeBase;
                    break;
                case string b when b.Contains("Sphere"):
                    areaObj.AreaShape = AreaRender.AreaType.Sphere;
                    break;
                case string c when c.Contains("Cylinder"):
                    areaObj.AreaShape = AreaRender.AreaType.CylinderBase;
                    break;
                default:
                    areaObj.AreaShape = AreaRender.AreaType.CubeBase;
                    break;
            }

            areaObj.Transform.UpdateMatrix(true);
            loader.AddRender(areaObj);
        }
        /// <summary>
        /// Gets the Actor's SARC, which can contain Model data as well as initialization info.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="modelArcs"></param>
        /// <returns></returns>
        private SARC GetActorArc(LiveActor actor, Dictionary<string, SARC> modelArcs)
        {
            string arcName = Path.GetFileName(actor.modelPath);

            if (!modelArcs.ContainsKey(arcName))
            {
                SARC arc = new SARC();

                arc.Load(new MemoryStream(YAZ0.Decompress(actor.modelPath)));

                modelArcs.Add(arcName, arc);
            }

            return modelArcs[arcName];
        }
        /// <summary>
        /// Gets the Actor's Model stream from the actor's loaded SARC
        /// </summary>
        /// <param name="modelArc"> Actor's main SARC, contains everything related to Actor init, including Model Data. </param>
        /// <param name="modelName"> Name of the Actor's Model file. </param>
        /// <returns> Memory Stream of the BFRES model if found, otherwise returns null. </returns>
        private Stream GetModelStream(SARC modelArc, string modelName)
        {
            ArchiveFileInfo modelFile = modelArc.files.Find(e => e.FileName.Contains($"{modelName}.bfres"));

            if (modelFile != null)
            {
                return modelFile.FileData;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Loads the Textures found in the Actor's Texture Archive if found in InitModel.byml
        /// </summary>
        /// <param name="modelARC"> Archive that the Actor uses for Initialization and Model data. </param>
        /// <param name="textureArcs"> Dictionary containing already obtained Texture Archives </param>
        /// <returns> Dictionary containing all textures found within the Texture Archive. </returns>
        private Dictionary<string, GenericRenderer.TextureView> GetTexArchive(SARC modelARC, Dictionary<string, Dictionary<string, GenericRenderer.TextureView>> textureArcs)
        {

            ArchiveFileInfo modelInfo = modelARC.files.Find(e => e.FileName == "InitModel.byml");

            if (modelInfo != null)
            {
                BymlFileData initModelByml = ByamlFile.LoadN(modelInfo.FileData, false);

                if (initModelByml.RootNode != null)
                {
                    if (initModelByml.RootNode is Dictionary<string, dynamic>)
                    {
                        if (((Dictionary<string, dynamic>)initModelByml.RootNode).ContainsKey("TextureArc"))
                        {
                            string texArcName = initModelByml.RootNode["TextureArc"];

                            if (!textureArcs.ContainsKey(texArcName))
                            {
                                string arcPath = $"{PluginConfig.GamePath}\\ObjectData\\{texArcName}.szs";

                                if (File.Exists(arcPath))
                                {
                                    SARC textureArc = new SARC();

                                    textureArc.Load(new MemoryStream(YAZ0.Decompress(arcPath)));

                                    ArchiveFileInfo texArcFile = textureArc.files.Find(e => e.FileName.Contains($"{texArcName}.bfres"));

                                    textureArcs.Add(texArcName, BfresLoader.GetTextures(texArcFile.FileData));
                                }
                            }
                            return textureArcs[texArcName];
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Adds objects to the scene.
        /// </summary>
        private void SetupObjects(EditorLoader loader)
        {

            Dictionary<string, SARC> modelArcs = new Dictionary<string, SARC>();
            Dictionary<string, Dictionary<string, GenericRenderer.TextureView>> textureArcs = new Dictionary<string, Dictionary<string, GenericRenderer.TextureView>>();

            foreach (var mapActors in loader.MapPlacementList["Scenario0"])
            {

                NodeBase actorList = new NodeBase(mapActors.Key);
                actorList.HasCheckBox = true;
                loader.Root.AddChild(actorList);
                actorList.Icon = IconManager.MODEL_ICON.ToString();

                foreach (var placement in mapActors.Value)
                {

                    LiveActor actor = new LiveActor(actorList, placement);

                    if(actor.hasArchive)
                    {
                        var modelARC = GetActorArc(actor, modelArcs);

                        var modelStream = GetModelStream(modelARC, Path.GetFileNameWithoutExtension(actor.modelPath));

                        if(modelStream != null)
                        {

                            actor.CreateBfresRenderer(modelStream, GetTexArchive(modelARC, textureArcs));

                        }else
                        {
                            actor.CreateBasicRenderer();
                        }
                    }
                    else
                    {
                        if(mapActors.Key == "AreaList" && actor.placement.ClassName.Contains("Area"))
                        {
                            actor.CreateAreaRenderer();
                        }else
                        {
                            actor.CreateBasicRenderer();
                        }
                    }

                    actor.Transform.UpdateMatrix(true);

                    loader.AddRender(actor);
                }
            }

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
    }
}
