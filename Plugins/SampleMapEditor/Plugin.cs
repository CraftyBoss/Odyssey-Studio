using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MapStudio.UI;
using GLFrameworkEngine;
using UIFramework;
using RedStarLibrary.UI;
using Toolbox.Core.IO;
using System.IO;
using ImGuiNET;
using System.Drawing;
using System.Numerics;
using System.IO.Pipes;

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

        public Plugin()
        {
            PluginConfig.Load();

            //var fileMenu = Framework.MainWindow.MenuItems.First(e=> e.Header == "File");

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

                if (editor is EditorLoader loader)
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

        private void ShowUploadFileDialog()
        {
            DialogHandler.Show("FTP Menu", 500, 400, DrawFTPMenu, (result) => { });
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
