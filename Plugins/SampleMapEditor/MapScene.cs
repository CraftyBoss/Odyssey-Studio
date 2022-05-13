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
        // Cant decide if this dialog should be used honestly
        const bool USE_SCENARIO_DIALOG = false;

        public void Setup(EditorLoader loader)
        {
            ProcessLoading.Instance.IsLoading = true;

            if (USE_SCENARIO_DIALOG)
            {
                DialogHandler.Show("Scenario Selection", 300, 100, () => {

                    ImGui.Text("Please Select a Starting Scenario.");
                    ImGui.DragInt("Scenario", ref loader.MapScenarioNo, 1);

                    if (ImGui.Button("Done"))
                    {
                        DialogHandler.ClosePopup(true);
                    }

                }, (isFinished) => {
                    //Prepare a collision caster for snapping objects onto
                    SetupSceneCollision();
                    //Add some objects to the scene
                    SetupObjects(loader);
                });
            }else
            {
                //Prepare a collision caster for snapping objects onto
                SetupSceneCollision();
                //Add some objects to the scene
                SetupObjects(loader);
            }

            ProcessLoading.Instance.IsLoading = false;
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

        private Stream GetInitFileStream(SARC modelArc, string initName)
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
            
            loader.MapActorList = new Dictionary<string, List<ActorList>>();

            int actorLoadedCount = 0;

            foreach (var mapLayer in LayerManager.LayerList)
            {

                List<ActorList> layerActors = new List<ActorList>();

                foreach (var mapActors in mapLayer.LayerObjects)
                {

                    ActorList actorCategory = new ActorList(mapActors.Key);

                    foreach (var placement in mapActors.Value)
                    {

                        LiveActor actor = new LiveActor(null, placement);

                        if (actor.hasArchive)
                        {
                            var modelARC = ResourceManager.FindOrLoadSARC(actor.modelPath);

                            var modelStream = GetModelStream(modelARC, Path.GetFileNameWithoutExtension(actor.modelPath));

                            if (modelStream != null)
                            {
                                BymlFileData clippingData = ByamlFile.LoadN(GetInitFileStream(modelARC, "InitClipping"));

                                if(((Dictionary<string,dynamic>)clippingData.RootNode).ContainsKey("Radius"))
                                {
                                    actor.IsInvalidateClipping = false;
                                    actor.ClippingDist = clippingData.RootNode["Radius"] * 10000;
                                }else if(((Dictionary<string, dynamic>)clippingData.RootNode).ContainsKey("Invalidate"))
                                {
                                    actor.IsInvalidateClipping = clippingData.RootNode["Invalidate"];
                                }

                                actor.CreateBfresRenderer(modelStream, GetTexArchive(modelARC));

                            }
                            else
                            {
                                actor.CreateBasicRenderer();
                            }
                        }
                        else
                        {
                            if (actor.placement.ClassName.Contains("Area") && actor.placement.ModelName != null && actor.placement.ModelName.StartsWith("Area"))
                            {
                                actor.ActorName = actor.placement.ClassName;
                                actor.CreateAreaRenderer();
                            }
                            else
                            {
                                actor.CreateBasicRenderer();
                            }
                        }

                        ProcessLoading.Instance.Update(actorLoadedCount, loader.MapActorCount, $"Loading Stage {loader.PlacementFileName}");

                        actorCategory.Add(actor);

                        actorLoadedCount++;

                    }

                    layerActors.Add(actorCategory);
                }

                loader.MapActorList.Add(mapLayer.LayerName, layerActors);

            }

            // Load Actor Models used in Current Scenario

            AddRendersInScenario(loader, loader.MapScenarioNo);

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

            AddRendersInScenario(loader, loader.MapScenarioNo);

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

            foreach (var mapActors in combinedList)
            {
                NodeBase actorList = new NodeBase(mapActors.ActorListName);
                actorList.HasCheckBox = true;
                loader.Root.AddChild(actorList);
                actorList.Icon = IconManager.MODEL_ICON.ToString();

                foreach (var actor in mapActors)
                {
                    actor.SetParentNode(actorList);
                    loader.AddRender(actor.ObjectRender);
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
