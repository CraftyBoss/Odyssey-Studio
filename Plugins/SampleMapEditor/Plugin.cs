using System;
using System.Collections.Generic;
using System.Linq;
using Toolbox.Core;
using MapStudio.UI;
using UIFramework;
using RedStarLibrary.UI;
using Toolbox.Core.IO;
using System.IO;
using ImGuiNET;
using System.Drawing;
using System.Numerics;
using RedStarLibrary.MapData;
using RedStarLibrary.Extensions;

namespace RedStarLibrary
{
    /// <summary>
    /// Represents a plugin for a map editor.
    /// This is required for every dll so the tool knows it is a valid plugin to use.
    /// </summary>
    public class Plugin : IPlugin
    {
        public string Name => "SMO Map Editor";

        private SwitchFileUploader Uploader = new SwitchFileUploader();

        private WorldList worldSelectorList = new WorldList();
        private bool isLoadedWorldList = false;
        private bool filterDemoStages = true;

        public Plugin()
        {
            PluginConfig.Load();

            UIManager.Subscribe(UIManager.UI_TYPE.NEW_FILE, "SMO Stage File", typeof(PlacementFileEditor));

            //var fileMenu = Framework.MainWindow.MenuItems.First(e=> e.Header == "File");

            Framework.MainWindow.MenuItems.Add(new MenuItem("Stage Select", ShowWorldSelectDialog));
            Framework.MainWindow.MenuItems.Add(new MenuItem("Upload Menu", ShowUploadFileDialog));
        }

        /// <summary>
        /// Draws settings used for the FTP Uploader
        /// </summary>
        private void DrawFTPMenu()
        {
            if(ImGui.TreeNode("FTP Settings"))
            {
                PluginConfig.DrawFTPSettings();

                if(Uploader.IsConnecting)
                {

                    ImGui.PushItemFlag(ImGuiItemFlags.Disabled, true);
                    ImGui.Button("Connecting...");
                    ImGui.PopItemFlag();
                }
                else
                {
                    if (!Uploader.IsConnected)
                    {
                        if (ImGui.Button("Connect"))
                            Uploader.ConnectToServer(PluginConfig.GetFTPInfo(), true);
                    }
                    else
                    {
                        if (ImGui.Button("Disconnect"))
                            Uploader.DisconnectFromServer();
                    }
                }

                bool curCheckValue = Uploader.IsWorkingDirAbsolute;
                if (ImGui.Checkbox("Is Working Directory Absolute", ref curCheckValue))
                    Uploader.IsWorkingDirAbsolute = curCheckValue;

                ImGui.TreePop();
            }

            if (!Uploader.IsConnected || Workspace.ActiveWorkspace == null || Workspace.ActiveWorkspace.ActiveEditor == null)
                return;

            if (ImGui.Button("Upload Stage File"))
            {
                var editor = Workspace.ActiveWorkspace.ActiveEditor;
                Uploader.WorkingDir = PluginConfig.FTPWorkingDir;

                if (editor is PlacementFileEditor loader)
                {
                    // make a local save
                    //STFileSaver.SaveFileFormat(editor);

                    using var mapDesign = new MemoryStream();
                    loader.SaveMap(mapDesign);
                    Uploader.UploadFileToServerAsync(YAZ0.Compress(mapDesign.ToArray(), Runtime.Yaz0CompressionLevel), loader.FileInfo.FileName);

                    if(loader.HasDesignFile)
                    {
                        using var designStream = new MemoryStream();
                        loader.SaveDesign(designStream);
                        Uploader.UploadFileToServerAsync(YAZ0.Compress(designStream.ToArray(), Runtime.Yaz0CompressionLevel), $"{loader.PlacementFileName}Design.szs");
                    }

                    if (loader.HasSoundFile)
                    {
                        using var soundStream = new MemoryStream();
                        loader.SaveSound(soundStream);
                        Uploader.UploadFileToServerAsync(YAZ0.Compress(soundStream.ToArray(), Runtime.Yaz0CompressionLevel), $"{loader.PlacementFileName}Sound.szs");
                    }
                }
                else if (editor is IFileFormat fileFormat)
                {
                    using Stream fileStream = SaveFileFormat(fileFormat);
                    Uploader.UploadFileToServerAsync(fileStream.ToArray(), fileFormat.FileInfo.FileName);
                }
            }

            if(Uploader.IsUploading)
            {
                ImGui.Text("Uploading: ");
                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, (uint)Color.Green.ToArgb());
                ImGui.ProgressBar(Uploader.UploadProgress, new Vector2(200.0f, 0.0f), $"{(int)Math.Floor(Uploader.UploadProgress * 100.0f)}%");
                ImGui.PopStyleColor();
            }
        }

        private void DrawWorldSelectorMenu()
        {
            if(!isLoadedWorldList)
            {
                var worldListSarc = ResourceManager.FindOrLoadSARC(Path.Combine("SystemData", "WorldList.szs"));

                worldSelectorList.DeserializeByml(new HakoniwaByml.Iter.BymlIter(worldListSarc.GetFileStream("WorldListFromDb.byml").ToArray()));

                isLoadedWorldList = true;
            }

            ImGui.Checkbox("Show Demo Stages", ref filterDemoStages);

            if(ImGui.BeginChild("WorldListWindow"))
            {
                foreach (var kingdomEntry in worldSelectorList.WorldEntries)
                {
                    var stageName = kingdomEntry.StageName;

                    IEnumerable<WorldList.Entry.StageEntry> filteredStageList;
                    if (filterDemoStages)
                        filteredStageList = kingdomEntry.StageList.Where(stageEntry => stageEntry.name != stageName && !stageEntry.name.StartsWith("Demo"));
                    else
                        filteredStageList = kingdomEntry.StageList.Where(stageEntry => stageEntry.name != stageName);

                    if (filteredStageList.Any())
                    {
                        bool treeOpen = ImGui.TreeNode(kingdomEntry.WorldName + " Kingdom");
                        ImGui.SameLine();
                        if (ImGui.Button($"Open Kingdom##{kingdomEntry.WorldName}"))
                            Framework.QueueWindowFileDrop(ResourceManager.FindResourcePath(Path.Combine("StageData", $"{stageName}Map.szs")));

                        if (treeOpen)
                        {
                            foreach (var stageEntry in filteredStageList)
                            {
                                ImGui.Bullet();
                                if (ImGui.Selectable(stageEntry.name))
                                    Framework.QueueWindowFileDrop(ResourceManager.FindResourcePath(Path.Combine("StageData", $"{stageEntry.name}Map.szs")));
                            }

                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        ImGui.Bullet();
                        if (ImGui.Selectable(kingdomEntry.WorldName + " Kingdom"))
                            Framework.QueueWindowFileDrop(ResourceManager.FindResourcePath(Path.Combine("StageData", $"{stageName}Map.szs")));
                    }
                }

                ImGui.EndChild();
            }
        }

        private void ShowUploadFileDialog()
        {
            DialogHandler.Show("FTP Menu", 500, 400, DrawFTPMenu, (result) => { });
        }

        private void ShowWorldSelectDialog()
        {
            DialogHandler.Show("Stage Selector", 500, 400, DrawWorldSelectorMenu, (result) => { });
        }

        public static Stream SaveFileFormat(IFileFormat fileFormat)
        {
            MemoryStream mem = new MemoryStream();
            fileFormat.Save(mem);

            if (fileFormat.FileInfo.Compression != null)
            {
                if(fileFormat.FileInfo.Compression is Yaz0)
                    return new MemoryStream(YAZ0.Compress(mem.ToArray(), Runtime.Yaz0CompressionLevel, (uint)((Yaz0)fileFormat.FileInfo.Compression).Alignment));
                else
                {
                    var compressionFormat = fileFormat.FileInfo.Compression;
                    var compressedStream = compressionFormat.Compress(mem);
                    //Update the compression size
                    fileFormat.FileInfo.CompressedSize = (uint)compressedStream.Length;
                    return compressedStream;
                }
            }

            return mem;
        }

        
    }
}
