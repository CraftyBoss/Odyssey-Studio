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
using Toolbox.Core.ViewModels;

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

        public Dictionary<string, List<ActorList>> MapActorList;

        public Dictionary<string, dynamic> MapGraphicsPreset;

        public int MapActorCount;

        /// <summary>
        /// The current Scenario selected for the loaded map.
        /// </summary>
        public static int MapScenarioNo { get; set; } = 0;

        /// <summary>
        /// Name of the currently loaded map without the Design/Map/Sound prefix
        /// </summary>
        public static string PlacementFileName { get; set; }

        /// <summary>
        /// Used to prevent LiveActors from removing/adding themselves from the layer config.
        /// </summary>
        public static bool IsReloadingStage { get; set; }

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

        const bool STAGE_TEST = false;

        /// <summary>
        /// Loads the given file data from a stream.
        /// </summary>
        public void Load(Stream stream)
        {
            //Set the game shader
            CafeLibrary.Rendering.BfresLoader.AddShaderType(typeof(SMORenderer));

            ObjectDatabaseGenerator.LoadDatabase();

            // Workspace.AddAssetCategory(new AssetLoaderLiveActor());

            // debug test every stage 

            if (STAGE_TEST)
            {
                var stageDirectory = new List<string>(Directory.GetFiles($"{PluginConfig.GamePath}\\StageData"));

                foreach (var stagePath in stageDirectory.FindAll(e => Path.GetFileNameWithoutExtension(e).Contains("StageMap") || Path.GetFileNameWithoutExtension(e).Contains("ZoneMap")))
                {

                    string fileName = Path.GetFileNameWithoutExtension(stagePath);

                    mapArc = new SARC();

                    mapArc.Load(new MemoryStream(YAZ0.Decompress(stagePath)));

                    ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains($"{fileName}.byml"));

                    LayerManager.CreateNewList();

                    if (mapData != null)
                    {
                        BymlFileData mapByml = ByamlFile.LoadN(mapData.FileData, false);

                        DeserializeMapData(mapByml.RootNode);

                        string designPath = ResourceManager.FindResourcePath($"StageData\\{PlacementFileName}Design.szs");

                        if (File.Exists(designPath))
                            LoadGraphicsData(designPath);

                        var tempCopy = LayerManager.GetAllObjectsInScenario(0);

                        foreach (var layer in LayerManager.LayerList)
                        {
                            foreach (var objList in layer.LayerObjects)
                            {
                                if (objList.Key == "AreaList" || objList.Key == "ZoneList") continue; // we should probably handle area's a bit differently

                                ObjectDatabaseGenerator.RegisterActorsToDatabase(objList.Value);
                            }
                        }

                        Console.WriteLine($"Stage {fileName} Loaded without Exception!");
                    }
                    else
                    {
                        throw new FileLoadException("Unable to Load Archive!");
                    }

                }

                ObjectDatabaseGenerator.SerializeDatabase();

                Console.WriteLine("Every Stage Sucessfully Loaded!");
            }
            else
            {
                mapArc = new SARC();

                mapArc.Load(stream);

                ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("StageDesign.byml") || e.FileName.Contains("StageSound.byml"));

                LayerManager.CreateNewList();

                if (mapData != null)
                {

                    BymlFileData mapByml = ByamlFile.LoadN(mapData.FileData, false);

                    DeserializeMapData(mapByml.RootNode);

                    string designPath = ResourceManager.FindResourcePath($"StageData\\{PlacementFileName}Design.szs");

                    if (File.Exists(designPath))
                        LoadGraphicsData(designPath);

                    CurrentMapScene = new MapScene();
                    CurrentMapScene.Setup(this);

                }
                else
                {
                    throw new FileLoadException("Unable to Load Archive!");
                }
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
            List<dynamic> otherserializedDict = new List<dynamic>();

            for (int i = 0; i < 15; i++) serializedDict.Add(new Dictionary<string, dynamic>());

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("StageDesign.byml") || e.FileName.Contains("StageSound.byml"));

            BymlFileData origMapByml = ByamlFile.LoadN(mapData.FileData, false);

            foreach (var actorLists in MapActorList)
            {
                actorLists.Value.ForEach(e => e.UpdateAllActorPlacement());
            }

            foreach (var layerconfig in LayerManager.LayerList)
            {
                // Console.WriteLine($"Layer Name: {layerconfig.LayerName}");

                foreach (var scenario in layerconfig.ScenarioList)
                {
                    // Console.WriteLine($"Scenario: {scenario}");

                    foreach (var objList in layerconfig.LayerObjects)
                    {

                        if (!serializedDict[scenario].ContainsKey(objList.Key))
                            serializedDict[scenario].Add(objList.Key, new List<dynamic>());

                        serializedDict[scenario][objList.Key].AddRange(Helpers.Placement.ConvertPlacementInfoList(objList.Value));

                    }
                }
            }

            mapByml.RootNode = serializedDict;

            // Console.WriteLine($"Saving File: {mapData.FileName}");

            var bymlData = ByamlFile.SaveN(mapByml);
            var memStream = new MemoryStream(bymlData);
            mapArc.SetFileData(mapData.FileName, memStream);

            mapArc.Save(stream);
        }

        private void LoadGraphicsData(string path)
        {

            var sarc = SARC_Parser.UnpackRamN(YAZ0.Decompress(path));

            BymlFileData gfxParam = ByamlFile.LoadN(new MemoryStream(sarc.Files["GraphicsArea.byml"]), false);

            var presetSarc = SARC_Parser.UnpackRamN(YAZ0.Decompress(ResourceManager.FindResourcePath("SystemData\\GraphicsPreset.szs")));

            foreach (Dictionary<string,dynamic> areaParam in gfxParam.RootNode["GraphicsAreaParamArray"])
            {
                // TODO: we shouldnt only get the first default area as there are stages that use different graphics presets according to scenario.
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

        public override List<MenuItemModel> GetViewMenuItems()
        {
            List<MenuItemModel> menuItemModels = new List<MenuItemModel>();

            menuItemModels.AddRange(new BFRES().GetViewMenuItems());

            return menuItemModels;
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

            var scenario = MapScenarioNo;
            ImGui.DragInt("Scenario", ref scenario, 1, 0, 14);
            MapScenarioNo = scenario;

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

        private void DeserializeMapData(List<dynamic> rootNode)
        {

            int scenarioNo = 0;

            foreach (Dictionary<string, dynamic> scenarioNode in rootNode)
            {

                List<PlacementInfo> linkedActorList = new List<PlacementInfo>();

                foreach (var actorListNode in scenarioNode)
                {
                    foreach (Dictionary<string, dynamic> actorNode in actorListNode.Value)
                    {
                        PlacementInfo actorPlacement = new PlacementInfo(actorNode);

                        if (!LayerManager.IsInfoInAnyLayer(actorPlacement))
                        {
                            if (PlacementFileName == null)
                                PlacementFileName = actorPlacement.PlacementFileName;

                            CreateAllActors(actorPlacement, linkedActorList, scenarioNo);

                            LayerManager.AddObjectToLayers(actorPlacement, scenarioNo, actorListNode.Key);
                        }
                        else
                        {
                            LayerManager.AddScenarioToLayer(actorPlacement.LayerConfigName, scenarioNo);
                        }
                    }
                }

                LayerManager.SetLayersAsLoaded();

                scenarioNo++;
            }
        }

        private void CreateAllActors(PlacementInfo actorPlacement, List<PlacementInfo> linkedActorList, int scenarioNo)
        {
            if (actorPlacement.isUseLinks)
            {
                foreach (var actorLink in actorPlacement.Links)
                {

                    actorPlacement.sourceLinks.Add(actorLink.Key, new List<PlacementInfo>());

                    foreach (Dictionary<string, dynamic> childActorNode in actorLink.Value)
                    {
                        PlacementInfo childPlacement = linkedActorList.Find(e => e.Id == childActorNode["Id"] && e.UnitConfigName == childActorNode["UnitConfigName"]);

                        if (childPlacement == null)
                        {
                            childPlacement = new PlacementInfo(childActorNode);
                            linkedActorList.Add(childPlacement);

                            CreateAllActors(childPlacement, linkedActorList, scenarioNo);
                        }

                        if (!childPlacement.destLinks.ContainsKey(actorLink.Key)) childPlacement.destLinks.Add(actorLink.Key, new List<PlacementInfo>());

                        childPlacement.destLinks[actorLink.Key].Add(actorPlacement);

                        actorPlacement.sourceLinks[actorLink.Key].Add(childPlacement);

                    }
                }
            }
        }
    }
}
