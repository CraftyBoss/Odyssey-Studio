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

namespace RedStarLibrary
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
        /// <returns> Dictionary containing all textures found within the Texture Archive. </returns>
        private Dictionary<string, GenericRenderer.TextureView> GetTexArchive(SARC modelARC)
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

                            return ResourceManager.FindOrLoadTextureList($"{PluginConfig.GamePath}\\ObjectData\\{texArcName}.szs");
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

            loader.MapActorList = new List<ActorList>();

            foreach (var mapActors in LayerManager.GetAllObjectsInScenario(loader.MapScenarioNo))
            {
                NodeBase actorList = new NodeBase(mapActors.Key);
                ActorList actorCategory = new ActorList(mapActors.Key);
                actorList.HasCheckBox = true;
                loader.Root.AddChild(actorList);
                actorList.Icon = IconManager.MODEL_ICON.ToString();

                foreach (var placement in mapActors.Value)
                {

                    LiveActor actor = new LiveActor(actorList, placement);

                    if(actor.hasArchive)
                    {
                        var modelARC = ResourceManager.FindOrLoadSARC(actor.modelPath);

                        var modelStream = GetModelStream(modelARC, Path.GetFileNameWithoutExtension(actor.modelPath));

                        if(modelStream != null)
                        {
                            actor.CreateBfresRenderer(modelStream, GetTexArchive(modelARC));

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

                    actorCategory.Add(actor);

                    loader.AddRender(actor.ObjectRender);
                    
                }

                loader.MapActorList.Add(actorCategory);

            }

            // Load Skybox

            //if(loader.MapGraphicsPreset != null)
            //{
            //    string arcName = loader.MapGraphicsPreset["Sky"]["Name"];

            //    string skyPath = $"{PluginConfig.GamePath}\\ObjectData\\{arcName}.szs";

            //    var skySarc = ResourceManager.FindOrLoadSARC(skyPath);

            //    NodeBase GraphicsObjs = new NodeBase("Graphics Objects");
            //    GraphicsObjs.HasCheckBox = true;
            //    loader.Root.AddChild(GraphicsObjs);
            //    GraphicsObjs.Icon = IconManager.MODEL_ICON.ToString();

            //    LiveActor skyActor = new LiveActor(GraphicsObjs, arcName, skyPath);

            //    skyActor.CreateBfresRenderer(GetModelStream(skySarc, arcName));

            //    ((BfresRender)skyActor.ObjectRender).UseDrawDistance = false;
            //    ((BfresRender)skyActor.ObjectRender).StayInFrustum = true;

            //    skyActor.ObjectRender.CanSelect = false;

            //    loader.AddRender(skyActor.ObjectRender);
            //}

        }

        public void RestartScene(EditorLoader loader)
        {

            loader.MapActorList = new List<ActorList>();

            for (int i = loader.Scene.Objects.Count - 1; i >= 0; i--)
            {
                loader.RemoveRender(loader.Scene.Objects[i]);
            }

            for (int i = loader.Root.Children.Count - 1; i >= 0; i--)
            {
                loader.Root.Children.Remove(loader.Root.Children[i]);
            }

            SetupObjects(loader);
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
