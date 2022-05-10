using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MapStudio.UI;
using GLFrameworkEngine;

namespace SampleMapEditor
{
    /// <summary>
    /// Represents a plugin for a map editor.
    /// This is required for every dll so the tool knows it is a valid plugin to use.
    /// </summary>
    public class Plugin : IPlugin
    {
        public string Name => "Sample Map Editor";

        public Plugin()
        {
            if(!PluginConfig.init)
                PluginConfig.Load();
        }
    }
}
