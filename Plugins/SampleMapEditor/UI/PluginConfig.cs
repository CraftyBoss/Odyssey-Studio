using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using Newtonsoft.Json;
using ImGuiNET;
using MapStudio.UI;
using Toolbox.Core;
using RedStarLibrary.UI;

namespace RedStarLibrary
{
    /// <summary>
    /// Represents UI for the plugin which is currently showing in the Paths section of the main menu UI.
    /// This is used to configure game paths.
    /// </summary>
    public class PluginConfig : IPluginConfig
    {
        [JsonObject]
        private struct ConfigData
        {
            public string GamePath = "";
            public string ModPath = "";
            public string FTPUsername = "";
            public string FTPPassword = "";
            public string FTPIp = "";
            public string FTPPort = "";
            public string FTPWorkingDir = "";

            public ConfigData() {}
        }

        private static bool isLoadedData = false;
        private static ConfigData configData = new ConfigData();

        public static string GamePath { get => configData.GamePath; }
        public static string ModPath { get => configData.ModPath; }
        public static string FTPUsername { get => configData.FTPUsername; }
        public static string FTPPassword { get => configData.FTPPassword; }
        public static string FTPIp { get => configData.FTPIp; }
        public static string FTPPort { get => configData.FTPPort; }
        public static string FTPWorkingDir { get => configData.FTPWorkingDir; }

        /// <summary>
        /// Renders the current configuration UI.
        /// </summary>
        public void DrawUI()
        {
            bool changed = false;

            if (ImguiCustomWidgets.PathSelector("Super Mario Odyssey Path", ref configData.GamePath))
                changed = true;
            if (ImguiCustomWidgets.PathSelector("Super Mario Odyssey Mod Path", ref configData.ModPath))
                changed = true;

            if (changed)
                Save();
        }

        public static void DrawFTPSettings()
        {
            bool changed = false;

            if (ImGui.InputText("FTP Username", ref configData.FTPUsername, 0x100))
                changed = true;
            if (ImGui.InputText("FTP Password", ref configData.FTPPassword, 0x100))
                changed = true;
            if (ImGui.InputText("FTP IP", ref configData.FTPIp, 0x100))
                changed = true;

            string ftpPortStr = configData.FTPPort;
            if (ImGui.InputText("FTP Port", ref ftpPortStr, 0x100) && ftpPortStr.All(char.IsDigit))
            {
                configData.FTPPort = ftpPortStr;
                changed = true;
            }

            if (ImGui.InputText("Server Working Dir", ref configData.FTPWorkingDir, 0x100))
                changed = true;

            if (changed)
                Save();
        }

        /// <summary>
        /// Loads the config json file on disc or creates a new one if it does not exist.
        /// </summary>
        /// <returns></returns>
        public static void Load() {
            if (isLoadedData)
                return;

            Console.WriteLine("Loading config...");
            if (!File.Exists($"{Runtime.ExecutableDir}\\SampleMapEditorConfig.json")) { Save(); }

            configData = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText($"{Runtime.ExecutableDir}\\SampleMapEditorConfig.json"));

            isLoadedData = true;
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public static void Save() {
            Console.WriteLine("Saving config...");
            File.WriteAllText($"{Runtime.ExecutableDir}\\SampleMapEditorConfig.json", JsonConvert.SerializeObject(configData));
            Reload();
        }

        /// <summary>
        /// Called when the config file has been loaded or saved.
        /// </summary>
        public static void Reload()
        {

        }

        public static FtpInfo GetFTPInfo()
        {
            return new FtpInfo()
            {
                addr = configData.FTPIp,
                user = configData.FTPUsername,
                pass = configData.FTPPassword,
                port = int.Parse(configData.FTPPort),
            };
        }
    }
}
