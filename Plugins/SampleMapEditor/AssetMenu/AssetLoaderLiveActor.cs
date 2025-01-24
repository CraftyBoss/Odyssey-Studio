using ImGuiNET;
using MapStudio.UI;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace RedStarLibrary.AssetMenu
{
    public class AssetLoaderLiveActor : IAssetLoader
    {
        public string Name => $"{category} List";
        private string category;

        public bool IsFilterMode => isFilter;
        private static bool isFilter = false;

        private List<AssetItem> assets;

        public AssetLoaderLiveActor(string category) : base()
        {
            this.category = category;

            List<AssetItem> iconAssets = new List<AssetItem>();
            List<AssetItem> emptyAssets = new List<AssetItem>();

            // organize by: actors with multiple icons -> actors with icons -> actors without icons

            var databaseCategory = ActorDataBase.GetDataBase().Where(e => e.ActorCategory == category);

            var categoryFolder = Path.Combine(Runtime.ExecutableDir, "Lib","Images","ActorThumbnails", category);

            foreach (var actor in databaseCategory)
            {
                if (actor.Models.Count > 1)
                {
                    var classFolder = Path.Combine(categoryFolder, actor.ClassName);

                    foreach (var item in actor.Models)
                        iconAssets.Add(new LiveActorAsset(actor, Path.Combine(classFolder, item + ".png")));
                }
                else if (actor.Models.Count == 1)
                {
                    iconAssets.Add(new LiveActorAsset(actor, Path.Combine(categoryFolder, actor.Models.FirstOrDefault() + ".png")));
                }
                else if (actor.Models.Count == 0)
                    emptyAssets.Add(new LiveActorAsset(actor));
            }

            assets = [.. iconAssets, .. emptyAssets];
        }

        public List<AssetItem> Reload()
        {
            return assets;
        }

        public bool UpdateFilterList()
        {
            bool filterUpdate = false;
            if (ImGui.Checkbox("Is Filter View", ref isFilter))
                filterUpdate = true;

            return filterUpdate;
        }
    }

    public class LiveActorAsset : AssetItem
    {
        public LiveActorAsset(ObjectDatabaseEntry entry, string icoPath = null) : base(icoPath)
        {
            DatabaseEntry = entry;

            if (icoPath == null)
            {
                ID = entry.ClassName;
                Name = entry.ClassName;

                icoPath = "Node";
            }else
            {
                Name = Path.GetFileNameWithoutExtension(icoPath);
            }

            if (!IconManager.HasIcon(icoPath)) 
                Console.WriteLine("Missing Icon at path: " + icoPath);

            Icon = IconManager.GetTextureIcon(icoPath);
        }
        public ObjectDatabaseEntry DatabaseEntry { get; private set; }
        public string ActorCategory { get { return DatabaseEntry.ActorCategory; } }

    }
}
