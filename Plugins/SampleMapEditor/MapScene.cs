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
using ImGuiNET;

namespace RedStarLibrary
{
    internal class MapScene
    {
        public void Setup(EditorLoader loader)
        {
            ProcessLoading.Instance.IsLoading = true;

            //Prepare a collision caster for snapping objects onto
            SetupSceneCollision();
            //Add some objects to the scene
            SetupObjects(loader);

            ProcessLoading.Instance.IsLoading = false;
        }

        // TODO: move these to ResourceManager or someplace better than here
        public static Stream GetModelStream(SARC modelArc, string modelName)
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

        public static Stream GetInitFileStream(SARC modelArc, string initName)
        {
            ArchiveFileInfo initFile = modelArc.files.Find(e => e.FileName.Contains($"{initName}.byml"));

            if (initFile != null)
                return initFile.FileData;
            else
                return null;
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

                            return ResourceManager.FindOrLoadTextureList($"ObjectData\\{texArcName}.szs");
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
            
            loader.MapActorList = new Dictionary<string, List<ActorList>>();

            int actorLoadedCount = 0;

            foreach (var mapLayer in LayerManager.LayerList)
            {

                List<ActorList> layerActors = new List<ActorList>();

                ActorList linkedActors = new ActorList("LinkedObjs");

                foreach (var mapActors in mapLayer.LayerObjects)
                {

                    ActorList actorCategory = new ActorList(mapActors.Key);

                    foreach (var placement in mapActors.Value)
                    {
                        actorCategory.Add(LoadActorFromPlacement(placement, linkedActors));

                        ProcessLoading.Instance.Update(actorLoadedCount, loader.MapActorCount, $"Loading Stage {EditorLoader.PlacementFileName}");

                        actorLoadedCount++;

                    }

                    layerActors.Add(actorCategory);

                }

                loader.MapActorList.Add(mapLayer.LayerName, layerActors);

            }

            // Load Actor Models used in Current Scenario

            AddRendersInScenario(loader, EditorLoader.MapScenarioNo);

            // Load Skybox

            if (loader.MapGraphicsPreset != null)
            {
                string arcName = loader.MapGraphicsPreset["Sky"]["Name"];

                string skyPath = ResourceManager.FindResourcePath($"ObjectData\\{arcName}.szs");

                var skySarc = ResourceManager.FindOrLoadSARC(skyPath);

                List<ActorList> graphicsObjs = new List<ActorList>();
                ActorList skyboxList = new ActorList("Skybox");
                graphicsObjs.Add(skyboxList);

                NodeBase GraphicsObjs = new NodeBase("Graphics Objects");
                GraphicsObjs.HasCheckBox = true;
                loader.Root.AddChild(GraphicsObjs);
                GraphicsObjs.Icon = IconManager.MODEL_ICON.ToString();

                LiveActor skyActor = new LiveActor(GraphicsObjs, arcName, skyPath);

                skyActor.CreateBfresRenderer(GetModelStream(skySarc, arcName));


                var bfresRender = ((BfresRender)skyActor.ObjectRender);

                bfresRender.UseDrawDistance = false;
                bfresRender.StayInFrustum = true;

                foreach (var tex in bfresRender.Textures.Values)
                    tex.AlphaChannel = STChannelType.One; // disable alpha channel

                skyActor.ObjectRender.CanSelect = false;

                skyboxList.Add(skyActor);

                loader.MapActorList.Add(GraphicsObjs.Header, graphicsObjs);

                loader.AddRender(skyActor.ObjectRender);
            }

        }

        public void RestartScene(EditorLoader loader)
        {

            EditorLoader.IsReloadingStage = true;

            for (int i = loader.Scene.Objects.Count - 1; i >= 0; i--)
            {
                loader.RemoveRender(loader.Scene.Objects[i]);
            }

            for (int i = loader.Root.Children.Count - 1; i >= 0; i--)
            {
                // remove actor lists in layer
                loader.Root.Children[i].Children.Clear();

                // remove layer from root
                loader.Root.Children.Remove(loader.Root.Children[i]);
            }

            AddRendersInScenario(loader, EditorLoader.MapScenarioNo);

            EditorLoader.IsReloadingStage = false;

        }

        public void AddRendersInScenario(EditorLoader loader, int scenario)
        {
            List<ActorList> combinedList = new List<ActorList>();

            foreach (var layer in LayerManager.GetLayersInScenario(scenario))
            {
                if(layer.IsEnabled && loader.MapActorList.ContainsKey(layer.LayerName))
                {
                    foreach (var layerActors in loader.MapActorList[layer.LayerName])
                    {
                        ActorList combinedActorList = combinedList.Find(e => e.ActorListName == layerActors.ActorListName);

                        if (combinedActorList == null)
                        {
                            combinedActorList = new ActorList(layerActors.ActorListName);
                            combinedList.Add(combinedActorList);
                        }

                        combinedActorList.AddRange(layerActors);

                    }
                }
            }

            if(loader.MapActorList.TryGetValue("Graphics Objects", out List<ActorList> objLists))
                foreach (var objList in objLists) combinedList.Add(objList);

            foreach (var mapActors in combinedList)
            {
                NodeBase actorList = new NodeBase(mapActors.ActorListName);
                actorList.HasCheckBox = true;
                loader.Root.AddChild(actorList);
                actorList.Icon = IconManager.FOLDER_ICON.ToString();

                foreach (var actor in mapActors)
                {
                    actor.ResetLinkedActors();

                    actor.SetParentNode(actorList);

                    if (actor.ObjectDrawer != null)
                        loader.AddRender(actor.ObjectDrawer);
                    else
                        loader.AddRender(actor.ObjectRender);

                    actor.isPlaced = true;

                    actor.PlaceLinkedObjects(loader);
                    
                }
            }
        }

        private LiveActor LoadActorFromPlacement(PlacementInfo placement, ActorList linkedActorList)
        {
            LiveActor actor = new LiveActor(null, placement);

            if (actor.hasArchive)
            {
                var modelARC = ResourceManager.FindOrLoadSARC(actor.modelPath);

                var modelStream = GetModelStream(modelARC, Path.GetFileNameWithoutExtension(actor.modelPath));

                if (modelStream != null)
                {
                    var fileStream = GetInitFileStream(modelARC, "InitClipping");

                    actor.CreateBfresRenderer(modelStream, GetTexArchive(modelARC));

                    if (fileStream != null)
                    {
                        BymlFileData clippingData = ByamlFile.LoadN(GetInitFileStream(modelARC, "InitClipping"));

                        if (((Dictionary<string, dynamic>)clippingData.RootNode).ContainsKey("Radius"))
                        {
                            actor.IsInvalidateClipping = false;
                            actor.ClippingDist = clippingData.RootNode["Radius"];
                        }
                        else if (((Dictionary<string, dynamic>)clippingData.RootNode).ContainsKey("Invalidate"))
                        {
                            actor.IsInvalidateClipping = clippingData.RootNode["Invalidate"];
                        }
                    }
                }
                else
                {
                    actor.CreateBasicRenderer();
                }
            }
            else
            {
                if (actor.Placement.ClassName.Contains("Area") && actor.Placement.ModelName != null && actor.Placement.ModelName.StartsWith("Area"))
                {
                    actor.ArchiveName = actor.Placement.ClassName;
                    actor.CreateAreaRenderer();
                }
                else if (actor.Placement.Id.Contains("rail")) // all rails use "rail" instead of "obj" for the id prefix
                {
                    actor.CreateRailRenderer();
                }
                else
                {
                    actor.CreateBasicRenderer();
                }
            }

            if(placement.isUseLinks)
            {
                foreach (var childPlacementList in placement.sourceLinks)
                {

                    List<LiveActor> linkedActors = new List<LiveActor>();

                    foreach (var childPlacement in childPlacementList.Value)
                    {
                        if (!childPlacement.isActorLoaded)
                        {
                            // load and add child actor to lists

                            LiveActor childActor = LoadActorFromPlacement(childPlacement, linkedActorList);

                            linkedActors.Add(childActor);
                            linkedActorList.Add(childActor);

                            if (!childActor.destLinkObjs.ContainsKey(childPlacementList.Key)) 
                                childActor.destLinkObjs.Add(childPlacementList.Key, new List<LiveActor>());

                            childActor.destLinkObjs[childPlacementList.Key].Add(actor);

                            childPlacement.isActorLoaded = true;

                        }else
                        {
                            // add already loaded actor to list
                            LiveActor childActor = linkedActorList.GetActorByPlacement(childPlacement);

                            if(childActor != null)
                            {
                                linkedActors.Add(childActor);

                                if (!childActor.destLinkObjs.ContainsKey(childPlacementList.Key))
                                    childActor.destLinkObjs.Add(childPlacementList.Key, new List<LiveActor>());

                                childActor.destLinkObjs[childPlacementList.Key].Add(actor);
                            }
                        }
                    }

                    actor.linkedObjs.Add(childPlacementList.Key, linkedActors);
                }
            
            }

            return actor;
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
