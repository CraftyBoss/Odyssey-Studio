using System;
using System.IO;
using Toolbox.Core;
using MapStudio.UI;
using OpenTK;
using GLFrameworkEngine;
using CafeLibrary;
using ByamlExt.Byaml;
using System.Collections.Generic;
using RedStarLibrary.GameTypes;
using RedStarLibrary.Rendering;
using Toolbox.Core.IO;
using RedStarLibrary.MapData;
using ImGuiNET;

namespace RedStarLibrary
{
    /// <summary>
    /// Represents a class used for loading files into the editor.
    /// IFileFormat determines what files to use. FileEditor is used to store all the editor information.
    /// </summary>
    public class EditorLoader : FileEditor, IFileFormat
    {
        /// <summary>
        /// The description of the file extension of the plugin.
        /// </summary>
        public string[] Description => new string[] { "Map Data" };

        /// <summary>
        /// The extension of the plugin. This should match whatever file you plan to open.
        /// </summary>
        public string[] Extension => new string[] { "*.szs" };

        /// <summary>
        /// Determines if the plugin can save or not.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// File info of the loaded file format.
        /// </summary>
        public File_Info FileInfo { get; set; }

        /// <summary>
        /// All Placement Info used in the loaded Stage
        /// </summary>
        public Dictionary<string, Dictionary<string, List<PlacementInfo>>> MapPlacementList;

        public Dictionary<string, List<ActorList>> MapActorList;

        public Dictionary<string, dynamic> MapGraphicsPreset;

        public int MapActorCount;

        /// <summary>
        /// The current Scenario selected for the loaded map.
        /// </summary>
        public int MapScenarioNo = 0;

        /// <summary>
        /// Name of the currently loaded map without the Design/Map/Sound prefix
        /// </summary>
        public string PlacementFileName;

        /// <summary>
        /// SARC containing all data used in the map
        /// </summary>
        private SARC mapArc;
        private MapScene CurrentMapScene { get; set; }

        /// <summary>
        /// Determines when to use the map editor from a given file.
        /// You can check from file extension or check the data inside the file stream.
        /// The file stream is always decompressed if the given file has a supported ICompressionFormat like Yaz0.
        /// </summary>
        public bool Identify(File_Info fileInfo, Stream stream)
        {
            return fileInfo.Extension == ".szs";
        }

        const bool USE_GAME_SHADERS = false;

        /// <summary>
        /// Loads the given file data from a stream.
        /// </summary>
        public void Load(Stream stream)
        {
            //Set the game shader
            if (USE_GAME_SHADERS)
            {
                CafeLibrary.Rendering.BfresLoader.AddShaderType(typeof(SMORenderer));
                CafeLibrary.PluginConfig.UseGameShaders = true;
            }

            mapArc = new SARC();

            mapArc.Load(stream);

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("StageDesign.byml") || e.FileName.Contains("StageSound.byml"));

            LayerManager.CreateNewList();

            if(mapData != null)
            {

                BymlFileData mapByml = ByamlFile.LoadN(mapData.FileData, false);

                int scenarioNo = 0;

                foreach (Dictionary<string, dynamic> scenarioNode in mapByml.RootNode as List<dynamic>)
                {

                    foreach (var actorListNode in scenarioNode)
                    {

                        foreach (Dictionary<string, dynamic> actorNode in actorListNode.Value)
                        {
                            PlacementInfo actorInfo = new PlacementInfo(actorNode);

                            if (PlacementFileName == null)
                            {
                                PlacementFileName = actorNode["PlacementFileName"];
                            }

                            if (actorInfo.isUseLinks)
                            {
                                CreateAllActors(actorInfo, scenarioNo, actorListNode.Key);
                            }
                            else
                            {
                                LayerManager.AddObjectToLayers(actorInfo, scenarioNo, actorListNode.Key);
                                MapActorCount++;
                            }
                        }
                    }

                    scenarioNo++;
                }

                string designPath = $"{PluginConfig.GamePath}\\StageData\\{PlacementFileName}Design.szs";

                if(File.Exists(designPath))
                {
                    LoadGraphicsData(designPath);
                }

                CurrentMapScene = new MapScene();
                CurrentMapScene.Setup(this);

            }
            else
            {
                throw new FileLoadException("Unable to Load Archive!");
            }
        }

        /// <summary>
        /// Saves the given file data to a stream.
        /// </summary>
        public void Save(Stream stream)
        {

            BymlFileData mapByml = new BymlFileData()
            {
                byteOrder = Syroot.BinaryData.ByteOrder.LittleEndian,
                SupportPaths = false,
                Version = 3
            };
            // List<Dictionary<string,List<Dictionary<string,dynamic>>>>
            List<dynamic> serializedDict = new List<dynamic>();

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("StageDesign.byml") || e.FileName.Contains("StageSound.byml"));

            BymlFileData origMapByml = ByamlFile.LoadN(mapData.FileData, false);

            int scenarioNo = 0;

            for(int i = 0; i < 15; i++)
            {
                var mapScenario = LayerManager.GetAllObjectsInScenario(i);

                Dictionary<string, dynamic> scenarioDict = new Dictionary<string, dynamic>();

                foreach (var actorLists in MapActorList)
                {
                    actorLists.Value.ForEach(e => e.UpdateAllActorPlacement());
                }

                foreach (var mapActorLists in mapScenario)
                {

                    if (mapActorLists.Key == "LinkedObjs") continue;

                    List<dynamic> actorList = new List<dynamic>();

                    foreach (var actorPlacementInfo in mapActorLists.Value)
                    {

                        actorPlacementInfo.SaveTransform();

                        actorList.Add(actorPlacementInfo.actorNode);
                    }

                    scenarioDict.Add(mapActorLists.Key, actorList);
                }

                serializedDict.Add(scenarioDict);

                scenarioNo++;
            }

            mapByml.RootNode = serializedDict;

            mapArc.SetFileData(PlacementFileName, new MemoryStream(ByamlFile.SaveN(mapByml)));

            mapArc.Save(stream);
        }

        private void LoadGraphicsData(string path)
        {

            var sarc = SARC_Parser.UnpackRamN(YAZ0.Decompress(path));

            BymlFileData gfxParam = ByamlFile.LoadN(new MemoryStream(sarc.Files["GraphicsArea.byml"]), false);

            var presetSarc = SARC_Parser.UnpackRamN(YAZ0.Decompress($"{PluginConfig.GamePath}\\SystemData\\GraphicsPreset.szs"));

            foreach (Dictionary<string,dynamic> areaParam in gfxParam.RootNode["GraphicsAreaParamArray"])
            {
                if(areaParam["AreaName"] == "DefaultArea" && areaParam["PresetName"] != null)
                {
                    byte[] paramBytes = null;
                    presetSarc.Files.TryGetValue($"{areaParam["PresetName"]}.byml", out paramBytes);

                    if(paramBytes != null)
                    {
                        BymlFileData gfxPreset = ByamlFile.LoadN(new MemoryStream(paramBytes), false);

                        MapGraphicsPreset = gfxPreset.RootNode;

                        break;
                    }
                }
            }

        }

        //Extra overrides for FileEditor you can use for custom UI

        /// <summary>
        /// Draws the viewport menu bar usable for custom tools.
        /// </summary>
        public override void DrawViewportMenuBar()
        {

        }

        private static bool isShowAddLayer = false;
        private static bool isShowAddNewLayer = false;
        private static bool isShowRemoveLayer = false;

        public override void DrawToolWindow()
        {
            if(ImGui.Button("Reload Scene"))
            {
                CurrentMapScene.RestartScene(this);
            }

            if(ImGui.Button("Open Stage Settings"))
            {
                DialogHandler.Show("Stage Settings", 400, 400, () => {

                    if (ImGui.Button("Close and Reload"))
                    {
                        DialogHandler.ClosePopup(true);
                    }

                    if (ImGui.CollapsingHeader($"Layer/Scenario Settings", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        DrawScenarioSettings();
                    }

                    if(ImGui.CollapsingHeader("Stage World List Settings", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        DrawWorldListSettings();
                    }

                }, (isDone) => {

                    CurrentMapScene.RestartScene(this);

                });
            }

        }

        private void DrawScenarioSettings()
        {
            var availableLayers = LayerManager.GetNamesNotInScenario(MapScenarioNo);

            var usedLayers = LayerManager.GetNamesInScenario(MapScenarioNo);

            ImGui.DragInt("Scenario", ref MapScenarioNo, 1, 0, 14);

            ImGuiHelper.BoldText("Scenario Layers:");

            ImGui.Columns(2, "layer_list", true);

            bool isNextColumn = false;

            for (int i = 0; i < usedLayers.Count; i++)
            {
                if (i > (usedLayers.Count - 1) / 2 && !isNextColumn)
                {
                    ImGui.NextColumn();
                    isNextColumn = true;
                }

                var curLayer = LayerManager.GetLayerByName(usedLayers[i]);
                ImGui.Checkbox(usedLayers[i], ref curLayer.IsEnabled);

            }

            ImGui.EndColumns();

            if (availableLayers.Count > 0)
            {
                if (ImGui.Button("Add Existing Layer") && !(isShowAddNewLayer || isShowRemoveLayer))
                {
                    isShowAddLayer = !isShowAddLayer;
                }

                ImGui.SameLine();
            }

            if (ImGui.Button("Add New Layer") && !(isShowAddLayer || isShowRemoveLayer))
            {
                isShowAddNewLayer = !isShowAddNewLayer;
            }

            ImGui.SameLine();

            if (ImGui.Button("Remove Layer") && !(isShowAddLayer || isShowAddNewLayer))
            {
                isShowRemoveLayer = !isShowRemoveLayer;
            }

            if (isShowAddLayer)
            {
                if (ImGui.BeginCombo("Available Layers", availableLayers[0]))
                {
                    foreach (var layerName in availableLayers)
                    {
                        if (ImGui.Selectable(layerName))
                        {
                            LayerManager.AddScenarioToLayer(layerName, MapScenarioNo);
                            isShowAddLayer = false;
                        }
                    }
                    ImGui.EndCombo();
                }
            }

            if (isShowAddNewLayer)
            {
                string layerName = "";
                if (ImGui.InputText("Layer Name", ref layerName, 512,
                    ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion |
                    ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.NoHorizontalScroll |
                    ImGuiInputTextFlags.AutoSelectAll))
                {
                    LayerManager.CreateNewConfig(layerName, MapScenarioNo);
                    isShowAddNewLayer = false;
                }
            }

            if (isShowRemoveLayer)
            {
                if (ImGui.BeginCombo("Active Layers", usedLayers[0]))
                {
                    foreach (var layerName in usedLayers)
                    {
                        if (ImGui.Selectable(layerName))
                        {
                            LayerManager.RemoveScenarioFromLayer(layerName, MapScenarioNo);
                            isShowRemoveLayer = false;
                        }
                    }
                    ImGui.EndCombo();
                }
            }
        }

        private void DrawWorldListSettings()
        {

        }

        /// <summary>
        /// When an asset item from the asset windows gets dropped into the editor.
        /// You can configure your own asset category from the asset window and make custom asset items to drop into.
        /// </summary>
        public override void AssetViewportDrop(AssetItem item, Vector2 screenPosition)
        {
            //viewport context
            var context = GLContext.ActiveContext;

            //Screen coords can be converted into 3D space
            //By default it will spawn in the mouse position at a distance
            Vector3 position = context.ScreenToWorld(screenPosition.X, screenPosition.Y, 100);
            //Collision dropping can be used to drop these assets to the ground from CollisionCaster
            if (context.EnableDropToCollision)
            {
                Quaternion rot = Quaternion.Identity;
                CollisionDetection.SetObjectToCollision(context, context.CollisionCaster, screenPosition, ref position, ref rot);
            }
        }

        /// <summary>
        /// Checks for dropped files to use for the editor.
        /// If the value is true, the file will not be loaded as an editor if supported.
        /// </summary>
        public override bool OnFileDrop(string filePath)
        {
            return false;
        }

        private void CreateAllActors(PlacementInfo actorInfo, int scenarioNo, string actorCategory)
        {

            LayerConfig layer = LayerManager.AddObjectToLayers(actorInfo, scenarioNo, actorCategory);
            MapActorCount++;

            List<PlacementInfo> linkedObjList = layer.LayerObjects[actorCategory];

            foreach (var linkList in actorInfo.Links)
            {
                foreach (Dictionary<string, dynamic> objNode in linkList.Value)
                {

                    if (!linkedObjList.Exists(e => e.ObjID == objNode["Id"]))
                    {
                        PlacementInfo childActorPlacement = new PlacementInfo(objNode);

                        if (childActorPlacement.isUseLinks)
                        {
                            CreateAllActors(childActorPlacement, scenarioNo, "LinkedObjs"); // recursively call function
                        }else
                        {
                            linkedObjList.Add(childActorPlacement);
                        }

                        linkedObjList.Add(childActorPlacement);

                    }
                }
            }

        }
    }
}
