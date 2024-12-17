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
using RedStarLibrary.Extensions;
using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using System.Linq;
using System.Text.Json;
using CafeLibrary.Rendering;

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

        public Dictionary<string, dynamic> MapGraphicsPreset;

        public int MapActorCount;

        /// <summary>
        /// Name of the currently loaded map without the Design/Map/Sound prefix
        /// </summary>
        public static string PlacementFileName { get; set; }

        /// <summary>
        /// Used to prevent LiveActors from removing/adding themselves from the layer config during stage loading.
        /// </summary>
        public static bool IsLoadingStage { get; set; }
        /// <summary>
        /// Currently selected Layer used for placing new objects into the scene.
        /// </summary>
        public static string SelectedLayer { get; set; } = "Common";

        /// <summary>
        /// SARC containing all data used in the map
        /// </summary>
        private SARC mapArc;
        private StageScene CurrentMapScene { get; set; }
        private string ThumbnailPath => $"{Runtime.ExecutableDir}\\Lib\\Images\\ActorThumbnails";

        /// <summary>
        /// Determines when to use the map editor from a given file.
        /// You can check from file extension or check the data inside the file stream.
        /// The file stream is always decompressed if the given file has a supported ICompressionFormat like Yaz0.
        /// </summary>
        public bool Identify(File_Info fileInfo, Stream stream)
        {
            return fileInfo.FileName.EndsWith("StageMap.szs") || fileInfo.FileName.EndsWith("ZoneMap.szs"); // temp support for zone maps, in future, auto load zones listed in ZoneList
        }

        /// <summary>
        /// Loads the given file data from a stream.
        /// </summary>
        public void Load(Stream stream)
        {

            //Set the game shader
            CafeLibrary.Rendering.BfresLoader.AddShaderType(typeof(SMORenderer));

            // add custom viewport item event

            Workspace.ViewportWindow.DrawEditorDropdown += (_,_) => { DrawEditorDropdown(); };

            ActorDataBase.LoadDatabase();

            if (InitIcons())
            {
                CreateAssetCategories();
            }

            mapArc = new SARC();

            mapArc.Load(stream);

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("ZoneMap.byml"));

            if (mapData != null)
            {

                PlacementFileName = mapData.FileName.Replace("Map.byml", "");

                BymlIter iter = new BymlIter(mapData.AsBytes());

                string designPath = ResourceManager.FindResourcePath($"StageData\\{PlacementFileName}Design.szs");

                if (File.Exists(designPath))
                    LoadGraphicsData(designPath);

                IsLoadingStage = true;

                CurrentMapScene = new StageScene();

                CurrentMapScene.DeserializeByml(iter);

                CurrentMapScene.Setup(this);

                IsLoadingStage = false;

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

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("StageDesign.byml") || e.FileName.Contains("StageSound.byml"));

            BymlWriter mapByml = new BymlWriter(CurrentMapScene.SerializeByml());

            //if(STAGE_TEST || stream == null)
            //{
            //    BymlFileData origMapByml = ByamlFile.LoadN(mapData.FileData, false);
            //
            //    if (Helpers.Placement.CompareStages(origMapByml.RootNode, serializedDict))
            //    {
            //        Console.ForegroundColor = ConsoleColor.Green;
            //        Console.WriteLine("New Stage matches original!");
            //    }
            //    else
            //    {
            //        Console.ForegroundColor = ConsoleColor.Red;
            //        Console.WriteLine("New Stage does not match original!");
            //    }
            //
            //    Console.ForegroundColor = ConsoleColor.Gray;
            //
            //    return;
            //}

            // Console.WriteLine($"Saving File: {mapData.FileName}");

            var memStream = new MemoryStream(mapByml.Serialize().ToArray());
            mapArc.SetFileData(mapData.FileName, memStream);

            mapArc.Save(stream);
        }

        // Iterates through every stage located within the users provided dump directory and creates a database with all objects found within the stages.
        private void GenerateActorDataBase()
        {
            var stageDirectory = new List<string>(Directory.GetFiles($"{PluginConfig.GamePath}\\StageData"));

            foreach (var stagePath in stageDirectory.FindAll(e => Path.GetFileNameWithoutExtension(e).Contains("StageMap")))
            {
                string fileName = Path.GetFileNameWithoutExtension(stagePath);

                mapArc = new SARC();

                mapArc.Load(new MemoryStream(YAZ0.Decompress(stagePath)));

                ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains($"{fileName}.byml"));

                if (mapData != null)
                {
                    StageScene curStage = new StageScene();

                    curStage.DeserializeByml(new BymlIter(mapData.AsBytes()));

                    //foreach (var layer in LayerList.LayerList)
                    //{
                    //    foreach (var objList in layer.LayerObjects)
                    //    {
                    //        if (objList.Key == "AreaList" || objList.Key == "ZoneList") continue; // we should probably handle area's a bit differently

                    //        ActorDataBase.RegisterActorsToDatabase(objList.Value);
                    //    }
                    //}

                    // global layers
                    foreach (var layers in curStage.GlobalLayers)
                    {
                        foreach (var layer in layers.Value)
                        {
                            ActorDataBase.RegisterActorsToDatabase(layer.LayerObjects);
                        }
                    }

                    // scenario layers
                    foreach (var scenario in curStage.StageScenarios)
                    {
                        foreach (var layers in scenario.ScenarioLayers)
                        {
                            foreach (var layer in layers.Value)
                            {
                                ActorDataBase.RegisterActorsToDatabase(layer.LayerObjects);
                            }
                        }
                    }
                }
            }

            ActorDataBase.SerializeDatabase();
        }

        private bool InitIcons()
        {
            if (Directory.Exists(ThumbnailPath))
            {
                foreach (var entry in ActorDataBase.GetDataBase())
                {
                    foreach (var modelName in entry.Models)
                    {
                        string fullPath = ThumbnailPath + $"\\{entry.ActorCategory}\\{entry.ClassName}\\{modelName}.png";

                        if (entry.Models.Count > 1)
                            fullPath = ThumbnailPath + $"\\{entry.ActorCategory}\\{entry.ClassName}\\{modelName}.png";
                        else
                            fullPath = ThumbnailPath + $"\\{entry.ActorCategory}\\{modelName}.png";

                        if (File.Exists(fullPath))
                            IconManager.LoadTextureFile(fullPath, 64, 64);
                    }
                    
                }

                return true;
            }

            return false;
        }

        private void CreateAssetCategories()
        {

            foreach (var thumbnailCategory in Directory.GetDirectories(ThumbnailPath))
            {
                var category = new AssetMenu.AssetLoaderLiveActor(Path.GetFileName(thumbnailCategory));

                Workspace.AddAssetCategory(category);
            }
        }

        private void LoadGraphicsData(string path)
        {

            var sarc = SARC_Parser.UnpackRamN(YAZ0.Decompress(path));

            //BymlFileData gfxParam = ByamlFile.LoadN(new MemoryStream(sarc.Files["GraphicsArea.byml"]), false);

            BymlIter gfxParam = new BymlIter(sarc.Files["GraphicsArea.byml"]);

            var presetSarc = SARC_Parser.UnpackRamN(YAZ0.Decompress(ResourceManager.FindResourcePath("SystemData\\GraphicsPreset.szs")));

            if(gfxParam.TryGetValue("GraphicsAreaParamArray", out BymlIter paramArray))
            {
                foreach(var graphicsIter in paramArray.AsArray<BymlIter>())
                {
                    // TODO: we shouldnt only get the first default area as there are stages that use different graphics presets according to scenario.
                    graphicsIter.TryGetValue("AreaName", out string areaName);
                    if (areaName == "DefaultArea" && graphicsIter.TryGetValue("PresetName", out string presetName))
                    {
                        byte[] paramBytes = null;
                        if (presetSarc.Files.TryGetValue($"{presetName}.byml", out paramBytes))
                        {
                            BymlFileData gfxPreset = ByamlFile.LoadN(new MemoryStream(paramBytes), false);
                            MapGraphicsPreset = gfxPreset.RootNode;
                            break;
                        }
                    }
                }
            }

            //foreach (Dictionary<string,dynamic> areaParam in gfxParam.RootNode["GraphicsAreaParamArray"])
            //{
            //    // TODO: we shouldnt only get the first default area as there are stages that use different graphics presets according to scenario.
            //    if(areaParam["AreaName"] == "DefaultArea" && areaParam["PresetName"] != null)
            //    {
            //        byte[] paramBytes = null;
            //        presetSarc.Files.TryGetValue($"{areaParam["PresetName"]}.byml", out paramBytes);

            //        if(paramBytes != null)
            //        {
            //            BymlFileData gfxPreset = ByamlFile.LoadN(new MemoryStream(paramBytes), false);

            //            MapGraphicsPreset = gfxPreset.RootNode;

            //            break;
            //        }
            //    }
            //}

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

            if(ImGui.Button("Dump Stage Models"))
            {
                var dlg = new ImguiFolderDialog();
                dlg.Title = "Select Stage Folder Output";

                if (dlg.ShowDialog())
                {
                    var stageActors = CurrentMapScene.GetLoadedActors();

                    var stageDumpPath = Path.Combine(dlg.SelectedPath, PlacementFileName);
                    if (!Directory.Exists(stageDumpPath))
                        Directory.CreateDirectory(stageDumpPath);

                    Dictionary<string, dynamic> stageData = new Dictionary<string, dynamic>();
                    Dictionary<string, dynamic> positionData = new Dictionary<string, dynamic>();
                    stageData.Add("PlacementInfo", positionData);

                    List<string> exportedModels = new List<string>();
                    stageData.Add("ExportedModels", exportedModels);

                    foreach (var actor in stageActors.Where(e => e.ObjectRender is BfresRender))
                    {
                        var placementInfo = actor.Placement;
                        var collectionName = $"{actor.ArchiveName}_{placementInfo.Id}";

                        Console.WriteLine("Adding Placement Info for: " + collectionName);

                        positionData.Add(collectionName, new Dictionary<string, dynamic>()
                        {
                            {"Position", placementInfo.Translate.ToDict() },
                            {"Rotation", placementInfo.Rotate.ToDict() },
                            {"Scale", placementInfo.Scale.ToDict() }
                        });

                        if (!exportedModels.Contains(actor.ArchiveName))
                        {
                            Console.WriteLine("Dumping Model: " + actor.ArchiveName);
                            actor.TryExportModel(stageDumpPath);
                            exportedModels.Add(actor.ArchiveName);
                        }
                    }
                    File.WriteAllText(Path.Combine(stageDumpPath, PlacementFileName + ".json"), JsonSerializer.Serialize(stageData, new JsonSerializerOptions() { WriteIndented = true }));
     
                    FileUtility.OpenFolder(stageDumpPath);
                }
            }
        }

        public void DrawEditorDropdown()
        {
            var w = ImGui.GetCursorPosX();

            var size = new System.Numerics.Vector2(160, ImGui.GetWindowHeight() - 1);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
            if (ImGui.Button($"Selected Layer: {SelectedLayer}"))
            {
                ImGui.OpenPopup("LayerList");
            }
            ImGui.PopStyleColor();

            var pos = ImGui.GetCursorScreenPos();

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(pos.X + w, pos.Y));

            if (ImGui.BeginPopup("LayerList"))
            {
                foreach (var menu in GetLayerMenuItems())
                    ImGuiHelper.LoadMenuItem(menu);
                ImGui.EndPopup();
            }
        }

        private List<MenuItemModel> GetLayerMenuItems()
        {
            List<MenuItemModel> layerModels = new List<MenuItemModel>();

            foreach (var layer in CurrentMapScene.GetLoadedLayers())
            {
                layerModels.Add(new MenuItemModel(layer, () =>
                {
                    SelectedLayer = layer;
                }, "", SelectedLayer == layer));
            }

            return layerModels;

        }

        private void DrawScenarioSettings()
        {
            //var availableLayers = LayerList.GetNamesNotInScenario(MapScenarioNo);

            //var usedLayers = LayerList.GetNamesInScenario(MapScenarioNo);

            var scenario = StageScene.MapScenarioNo;
            ImGui.DragInt("Scenario", ref scenario, 1, 0, 14);
            StageScene.MapScenarioNo = scenario;

            //ImGuiHelper.BoldText("Scenario Layers:");

            //ImGui.Columns(2, "layer_list", true);

            //bool isNextColumn = false;

            //for (int i = 0; i < usedLayers.Count; i++)
            //{
            //    if (i > (usedLayers.Count - 1) / 2 && !isNextColumn)
            //    {
            //        ImGui.NextColumn();
            //        isNextColumn = true;
            //    }

            //    var curLayer = LayerList.GetLayerByName(usedLayers[i]);
            //    ImGui.Checkbox(usedLayers[i], ref curLayer.IsEnabled);

            //}

            //ImGui.EndColumns();

            //if (availableLayers.Count > 0)
            //{
            //    if (ImGui.Button("Add Existing Layer") && !(isShowAddNewLayer || isShowRemoveLayer))
            //    {
            //        isShowAddLayer = !isShowAddLayer;
            //    }

            //    ImGui.SameLine();
            //}

            //if (ImGui.Button("Add New Layer") && !(isShowAddLayer || isShowRemoveLayer))
            //{
            //    isShowAddNewLayer = !isShowAddNewLayer;
            //}

            //ImGui.SameLine();

            //if (ImGui.Button("Remove Layer") && !(isShowAddLayer || isShowAddNewLayer))
            //{
            //    isShowRemoveLayer = !isShowRemoveLayer;
            //}

            //if (isShowAddLayer)
            //{
            //    if (ImGui.BeginCombo("Available Layers", availableLayers[0]))
            //    {
            //        foreach (var layerName in availableLayers)
            //        {
            //            if (ImGui.Selectable(layerName))
            //            {
            //                LayerList.AddScenarioToLayer(layerName, MapScenarioNo);
            //                isShowAddLayer = false;
            //            }
            //        }
            //        ImGui.EndCombo();
            //    }
            //}

            //if (isShowAddNewLayer)
            //{
            //    string layerName = "";
            //    if (ImGui.InputText("Layer Name", ref layerName, 512,
            //        ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion |
            //        ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.NoHorizontalScroll |
            //        ImGuiInputTextFlags.AutoSelectAll))
            //    {
            //        LayerList.CreateNewConfig(layerName, MapScenarioNo);

            //        MapActorList.Add(layerName, new List<ActorList>());

            //        isShowAddNewLayer = false;
            //    }
            //}

            //if (isShowRemoveLayer)
            //{
            //    if (ImGui.BeginCombo("Active Layers", usedLayers[0]))
            //    {
            //        foreach (var layerName in usedLayers)
            //        {
            //            if (ImGui.Selectable(layerName))
            //            {
            //                LayerList.RemoveScenarioFromLayer(layerName, MapScenarioNo);
            //                isShowRemoveLayer = false;
            //            }
            //        }
            //        ImGui.EndCombo();
            //    }
            //}
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

            if(item is AssetMenu.LiveActorAsset actorAsset)
            {

                CurrentMapScene.TryAddActorFromAsset(this, position, actorAsset);

                //var actorPlacement = new PlacementInfo(actorAsset.DatabaseEntry);

                //Random rng = new Random();

                //string listName = actorPlacement.UnitConfig.GenerateCategory;

                //actorPlacement.Translate = position;
                //actorPlacement.Id = "obj" + rng.Next(1000);
                //actorPlacement.LayerConfigName = SelectedLayer;
                //actorPlacement.PlacementFileName = PlacementFileName;

                //var actor = CurrentMapScene.LoadActorFromPlacement(actorPlacement, new ActorList(""));

                //if(MapActorList.TryGetValue(SelectedLayer, out List<ActorList> lists))
                //{
                //    var actorList = lists.Find(e => e.ActorListName == listName);

                //    if(actorList != null)
                //    {
                //        actorList.Add(actor);
                //    }else
                //    {
                //        actorList = new ActorList(listName);
                //        lists.Add(actorList);
                //    }
                //}else
                //{
                //    var actorLists = new List<ActorList>();

                //    actorLists.Add(new ActorList(listName));

                //    MapActorList.Add(SelectedLayer, actorLists);
                //}

                //AddActorToRender(actor);
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

        //public void AddActorToRender(LiveActor actor)
        //{
        //    string actorListName = actor.Placement.UnitConfig.GenerateCategory;

        //    NodeBase actorNode = Root.GetChild(actorListName);

        //    if (actorNode == null)
        //    {
        //        actorNode = new NodeBase(actorListName);
        //        actorNode.HasCheckBox = true;
        //        Root.AddChild(actorNode);
        //        actorNode.Icon = IconManager.FOLDER_ICON.ToString();
        //    }

        //    actor.ResetLinkedActors();

        //    actor.SetParentNode(actorNode);

        //    if (actor.ObjectDrawer != null)
        //        AddRender(actor.ObjectDrawer);
        //    else
        //        AddRender(actor.ObjectRender);

        //    actor.isPlaced = true;

        //    // actor.PlaceLinkedObjects(this);

        //    var context = GLContext.ActiveContext;
        //    context.Camera.FocusOnObject(actor.Transform);
        //}

        
    }
}
