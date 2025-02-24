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
using RedStarLibrary.MapData.Camera;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using RedStarLibrary.Helpers;
using RedStarLibrary.MapData.Graphics;
using BfresLibrary;
using Newtonsoft.Json.Linq;


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
        public int Priority => -1;
        public int MapActorCount { get; private set; }
        public bool HasDesignFile => designArc != null;
        public bool HasSoundFile => soundArc != null;
        /// <summary>
        /// Name of the currently loaded map without the Design/Map/Sound prefix
        /// </summary>
        public string PlacementFileName { get; set; }
        /// <summary>
        /// Used to prevent LiveActors from removing/adding themselves from the layer config during stage loading.
        /// </summary>
        public static bool IsLoadingStage { get; set; }
        /// <summary>
        /// Currently selected Layer used for placing new objects into the scene.
        /// </summary>
        public static string SelectedLayer { get; set; } = "Common";
        public StageScene CurrentMapScene { get; private set; }

        /// <summary>
        /// SARC containing all data used in the map
        /// </summary>
        private SARC mapArc;
        private SARC designArc;
        private SARC soundArc;
        private SARC presetSarc;

        // layer menu variables
        private string editLayerName = "";
        private string newLayerName = "";
        private bool isLayerEditTakeFocus = false;

        private string ThumbnailPath => $"{Runtime.ExecutableDir}\\Lib\\Images\\ActorThumbnails";

        public static Dictionary<string, Dictionary<string, string>> TempCameraParamCollection = new();

        public EditorLoader() : base() 
        { 
            Root.Tag = this;
        }

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
            //foreach (var stageFilePath in Directory.GetFiles(ResourceManager.FindResourcePath($"StageData"), "*Map.szs"))
            //{
            //    SarcData stageSarc = new SarcData();
            //    var cameraParamData = SARC.TryGetFile(stageFilePath, "CameraParam.byml");
            //    if (cameraParamData.Length == 0)
            //        continue;
            //    var cameraParamIter = new BymlIter(cameraParamData);
            //    var cameraParam = new CameraParam();
            //    cameraParam.DeserializeByml(cameraParamIter);
            //}
            //File.WriteAllText("FoundParams.json", JsonSerializer.Serialize(TempCameraParamCollection, new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)}));

            //Set the game shader
            BfresLoader.TargetShader = typeof(SMORenderer);

            // add custom viewport item event

            Workspace.ViewportWindow.DrawEditorDropdown += (_,_) => { DrawEditorDropdown(); };

            ActorDataBase.LoadDatabase();

            if (InitIcons())
                CreateAssetCategories();

            mapArc = new SARC();

            mapArc.Load(stream);

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("ZoneMap.byml"));

            if (mapData != null)
            {
                PlacementFileName = mapData.FileName.Replace("Map.byml", "");

                BymlIter iter = new BymlIter(mapData.AsBytes());

                IsLoadingStage = true;

                CurrentMapScene = new StageScene();

                CurrentMapScene.DeserializeByml(iter);

                string designPath = Path.Combine(FileInfo.FolderPath, $"{PlacementFileName}Design.szs");
                if (File.Exists(designPath))
                    LoadGraphicsData(designPath);

                string soundPath = Path.Combine(FileInfo.FolderPath, $"{PlacementFileName}Sound.szs");
                if (File.Exists(soundPath))
                    LoadSoundData(soundPath);

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
            // update file name upon save
            PlacementFileName = Path.GetFileName(FileInfo.FilePath);
            FileInfo.FileName = PlacementFileName;

            if (PlacementFileName.EndsWith("Map.szs"))
                PlacementFileName = PlacementFileName.Remove(PlacementFileName.LastIndexOf("Map.szs"));
            else
                throw new Exception("Stage file must be saved with the suffix \"Map\" in order to be valid!");

            // TODO: update placementfilename entry in placementinfos when serializing

            SaveMap(stream);

            if(HasDesignFile)
            {
                using var designStream = new MemoryStream();
                SaveDesign(designStream);
                File.WriteAllBytes(Path.Combine(FileInfo.FolderPath, $"{PlacementFileName}Design.szs"), YAZ0.Compress(designStream.ToArray(), Runtime.Yaz0CompressionLevel));
            }
            if(HasSoundFile)
            {
                using var soundStream = new MemoryStream();
                SaveSound(soundStream);
                File.WriteAllBytes(Path.Combine(FileInfo.FolderPath, $"{PlacementFileName}Sound.szs"), YAZ0.Compress(soundStream.ToArray(), Runtime.Yaz0CompressionLevel));
            }

            //{
            //    var origMapIter = new BymlIter(mapData.FileData.ToArray());
            //    File.WriteAllText(Path.Combine(FileInfo.FolderPath, "OrigStageByml.yaml"), origMapIter.ToYaml());
            //    File.WriteAllText(Path.Combine(FileInfo.FolderPath, "NewStageByml.yaml"), mapContainer.ToYaml());
            //    if (Helpers.Placement.CompareStages(origMapIter, new BymlIter(memStream.ToArray())))
            //    {
            //        Console.ForegroundColor = ConsoleColor.Green;
            //        Console.WriteLine("New Stage matches original!");
            //    }
            //    else
            //    {
            //        Console.ForegroundColor = ConsoleColor.Red;
            //        Console.WriteLine("New Stage does not match original!");
            //    }
            //    Console.ForegroundColor = ConsoleColor.Gray;
            //}
        }

        public void SaveMap(Stream stream)
        {
            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.EndsWith("Map.byml"));

            string arcName = $"{PlacementFileName}Map.byml";

            var mapContainer = CurrentMapScene.SerializeByml();
            BymlWriter mapByml = new BymlWriter(mapContainer);
            var memStream = new MemoryStream(mapByml.Serialize().ToArray());

            Console.WriteLine($"Saving File: {arcName}");

            if(mapData.FileName != arcName)
            {
                mapArc.DeleteFile(mapData); // remove previous file

                mapArc.AddFile(arcName, memStream);
            }
            else
                mapArc.SetFileData(arcName, memStream);

            mapArc.Save(stream);
        }

        public void SaveDesign(Stream stream)
        {
            if (!HasDesignFile)
                return;

            Console.WriteLine($"Saving Graphics Area Data.");

            BymlWriter graphicsAreaByml = new BymlWriter(CurrentMapScene.GraphicsArea.SerializeByml());
            designArc.SetFileData("GraphicsArea.byml", new MemoryStream(graphicsAreaByml.Serialize().ToArray()));

            // update design arc with placement name
            ArchiveFileInfo designData = designArc.files.Find(e => e.FileName.EndsWith("Design.byml"));
            designArc.RenameFile(designData, $"{PlacementFileName}Design.byml");

            designArc.Save(stream);
        }

        public void SaveSound(Stream stream)
        {
            if (!HasSoundFile)
                return;

            // update sound arc with placement name
            ArchiveFileInfo soundData = soundArc.files.Find(e => e.FileName.EndsWith("Sound.byml"));
            soundArc.RenameFile(soundData, $"{PlacementFileName}Sound.byml");

            soundArc.Save(stream);
        }

        public bool TryGetPresetFromArc(string presetName, out Dictionary<string, dynamic> result)
        {
            var presetFile = presetSarc.GetFileStream($"{presetName}.byml");

            if (presetFile != null)
            {
                var gfxPresetIter = new BymlIter(presetFile.ToArray());
                result = Placement.ConvertToDict(gfxPresetIter);
                return true;
            }
            result = null;
            return false;
        }

        // Iterates through every stage located within the users provided dump directory and creates a database with all objects found within the stages.
        private void GenerateActorDataBase()
        {
            // TODO: re-generate database with type info for each parameter

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
            Dictionary<string, AssetMenu.AssetLoaderLiveActor> categories = new();
            foreach (var obj in ActorDataBase.ObjDatabase)
            {
                if(!categories.ContainsKey(obj.ActorCategory)) {
                    categories.Add(obj.ActorCategory, new AssetMenu.AssetLoaderLiveActor(obj.ActorCategory));
                }
            }

            foreach (var category in categories)
                Workspace.AddAssetCategory(category.Value);
        }

        private void LoadGraphicsData(string path)
        {
            designArc = new SARC();
            designArc.Load(new MemoryStream(YAZ0.Decompress(path)));
            //designArc.Load(new FileStream(path, FileMode.Open));

            presetSarc = new SARC();
            presetSarc.Load(new MemoryStream(YAZ0.Decompress(ResourceManager.FindResourcePath("SystemData\\GraphicsPreset.szs"))));

            if (!designArc.files.Any(e => e.FileName == "GraphicsArea.byml"))
                return;

            BymlIter gfxParam = new BymlIter(designArc.GetFileStream("GraphicsArea.byml").ToArray());

            CurrentMapScene.SetupGraphicsArea(gfxParam);
        }

        private void LoadSoundData(string path)
        {
            soundArc = new SARC();
            soundArc.Load(new MemoryStream(YAZ0.Decompress(path)));
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

        public override void DrawToolWindow()
        {
            System.Numerics.Vector2 btnSize = new System.Numerics.Vector2(ImGui.GetWindowWidth(), 23);

            if (ImGui.Button("Reload Scene", btnSize))
                CurrentMapScene.RestartScene(this);

            if(ImGui.Button("Open Stage Settings", btnSize))
            {
                DialogHandler.Show("Stage Settings", 500, 400, () => {

                    System.Numerics.Vector2 settingsBtnSize = new System.Numerics.Vector2(ImGui.GetWindowWidth(), 23);
                    if (ImGui.Button("Close and Reload", settingsBtnSize))
                        DialogHandler.ClosePopup(true);

                    if (ImGui.CollapsingHeader($"Layer/Scenario Settings", ImGuiTreeNodeFlags.DefaultOpen))
                        DrawScenarioSettings();

                    if(ImGui.CollapsingHeader("Stage World List Settings", ImGuiTreeNodeFlags.DefaultOpen))
                        DrawWorldListSettings();

                    if (ImGui.CollapsingHeader("Stage Graphics Settings", ImGuiTreeNodeFlags.DefaultOpen))
                        DrawGraphicsSettings();

                }, (isDone) => {
                    if(isDone)
                        CurrentMapScene.RestartScene(this);
                });
            }

            if(ImGui.Button("Dump Stage Models", btnSize))
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
            var scenario = CurrentMapScene.MapScenarioNo;
            ImGui.DragInt("Scenario", ref scenario, 1, 0, 14);
            CurrentMapScene.MapScenarioNo = scenario;

            ImGuiHelper.BoldText("Scenario Layers");
            ImGui.Separator();

            foreach ((var categoryName, var categoryList) in CurrentMapScene.GlobalLayers)
            {
                if (ImGui.BeginMenu(categoryName))
                {
                    if (ImGui.BeginTable("LayerScenarioTable", StageScene.SCENARIO_COUNT + 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);

                        ImGui.Text("Layer Name");
                        for (int i = 0; i < StageScene.SCENARIO_COUNT; i++)
                        {
                            ImGui.TableSetColumnIndex(i + 1);
                            ImGui.Text((i+1).ToString());
                        }

                        List<LayerConfig> removeQueue = new List<LayerConfig>();

                        foreach (var layer in categoryList)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);

                            string layerStrId = $"{categoryName}_{layer.LayerName}";

                            if (editLayerName == layerStrId)
                            {
                                if (isLayerEditTakeFocus)
                                    ImGui.SetKeyboardFocusHere(0);

                                bool isFinished = false;

                                if (ImGui.InputText("##EditLayerName", ref newLayerName, 0x40, ImGuiInputTextFlags.EnterReturnsTrue))
                                {
                                    layer.SetLayerName(newLayerName);
                                    isFinished = true;
                                }

                                bool isLostFocus = !isLayerEditTakeFocus && !ImGui.IsItemFocused();
                                if (isLostFocus || isFinished)
                                {
                                    editLayerName = "";
                                    newLayerName = "";
                                }

                                isLayerEditTakeFocus = false;
                            }
                            else
                            {
                                // TODO: make this more obviously editable
                                ImGui.Text(layer.LayerName);

                                if (ImGui.IsItemHovered())
                                    ImGui.SetTooltip("Double click to rename, Right click to Edit");

                                if(ImGui.BeginPopupContextItem($"{layerStrId}_PopupCtx"))
                                {
                                    if(ImGui.Button("Set All Active"))
                                    {
                                        for (int i = 0; i < StageScene.SCENARIO_COUNT; i++)
                                            layer.SetScenarioActive(i, true);
                                    }
                                    if (ImGui.Button("Set All Inactive"))
                                    {
                                        for (int i = 0; i < StageScene.SCENARIO_COUNT; i++)
                                            layer.SetScenarioActive(i, false);
                                    }

                                    if (ImGui.Button("Remove"))
                                        removeQueue.Add(layer);

                                    ImGui.EndPopup();
                                }

                                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                                {
                                    editLayerName = layerStrId;
                                    isLayerEditTakeFocus = true;
                                }
                            }
                            
                            for (int i = 0; i < StageScene.SCENARIO_COUNT; i++)
                            {
                                ImGui.TableSetColumnIndex(i + 1);

                                bool isChecked = layer.IsScenarioActive(i);
                                if (ImGui.Checkbox($"##{layerStrId}_Checkbox{i}", ref isChecked))
                                    layer.SetScenarioActive(i, isChecked);
                            }
                        }

                        foreach (var layer in removeQueue)
                            categoryList.RemoveLayer(layer);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);

                        System.Numerics.Vector2 btnSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 23);
                        if (ImGui.Button($"Add##LayerCreateButton", btnSize))
                        {
                            int newCount = categoryList.GetLayersBySubString("NewLayer").Count();
                            categoryList.FindOrCreateLayer($"NewLayer{(newCount > 0 ? '_' + newCount : "")}");
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndMenu();
                }
                
            }
        }

        private void DrawWorldListSettings()
        {

        }

        private void DrawGraphicsSettings()
        {
            ImGuiHelper.BoldText("Graphics Area Parameters");
            ImGui.Separator();
            System.Numerics.Vector2 btnSize = new System.Numerics.Vector2(ImGui.GetWindowWidth(), 23);

            if (ImGui.Button("Add Area Parameter", btnSize))
                CurrentMapScene.GraphicsArea.AddNewParam();

            if (ImGui.BeginChild("AreaParamChild", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 150), true))
            {
                List<StageGraphicsArea.AreaParam> removeParams = new List<StageGraphicsArea.AreaParam>();

                int idx = 0;
                foreach (var areaParam in CurrentMapScene.GraphicsArea.ParamArray)
                {
                    bool isOpen = ImGui.TreeNode($"Param {1 + (idx++)}");
                    ImGui.SameLine();

                    if (ImGui.SmallButton("Remove"))
                        removeParams.Add(areaParam);

                    if (isOpen)
                    {
                        ImGui.InputText("Preset Name", ref areaParam.PresetName, 0x100);
                        ImGui.InputText("Area Name", ref areaParam.AreaName, 0x100);

                        ImGui.InputText("CubeMap Unit Name", ref areaParam.CubeMapUnitName, 0x100);
                        ImGui.InputText("Suffix", ref areaParam.SuffixName, 0x100);

                        if(areaParam.AdditionalCubeMapArchiveName != null)
                            ImGui.InputText("Additional CubeMap Archive Name", ref areaParam.AdditionalCubeMapArchiveName, 0x100);
                        if (areaParam.AdditionalCubeMapUnitName != null)
                            ImGui.InputText("Additional CubeMap Unit Name", ref areaParam.AdditionalCubeMapUnitName, 0x100);

                        var baseAngle = new System.Numerics.Vector3(areaParam.BaseAngle.X, areaParam.BaseAngle.Y, areaParam.BaseAngle.Z);
                        if(ImGui.DragFloat3("Base Angle", ref baseAngle))
                        {
                            areaParam.BaseAngle.X = baseAngle.X;
                            areaParam.BaseAngle.Y = baseAngle.Y;
                            areaParam.BaseAngle.Z = baseAngle.Z;
                        }

                        ImGui.InputInt("Lerp Step", ref areaParam.LerpStep);
                        ImGui.InputInt("Lerp Step Out", ref areaParam.LerpStepOut);

                        ImGui.TreePop();
                    }
                }

                foreach (var removeParam in removeParams)
                    CurrentMapScene.GraphicsArea.ParamArray.Remove(removeParam);

                ImGui.EndChild();
            }
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
                CurrentMapScene.TryAddActorFromAsset(this, position, actorAsset);
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
