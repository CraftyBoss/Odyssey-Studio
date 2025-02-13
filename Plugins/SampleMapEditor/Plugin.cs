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

            var fileMenu = Framework.MainWindow.MenuItems.First(e=> e.Header == "File");

            fileMenu.MenuItems.Add(new MenuItem("Upload File") { RenderItems = () =>
            {

            }});

            Framework.MainWindow.MenuItems.Add(new MenuItem("FTP Settings") { RenderItems = PluginConfig.DrawFTPSettings });
        }
    }
}
