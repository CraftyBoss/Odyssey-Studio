using ImGuiNET;
using MapStudio.UI;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
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

            List<AssetItem> folderAssets = new List<AssetItem>();
            List<AssetItem> iconAssets = new List<AssetItem>();
            List<AssetItem> emptyAssets = new List<AssetItem>();

            // organize by: actors with multiple icons -> actors with icons -> actors without icons

            var databaseCategory = ActorDataBase.GetDataBase().Where(e => e.ActorCategory == category);

            foreach (var actor in databaseCategory)
            {
                if (actor.Models.Count > 1)
                    folderAssets.Add(CreateAsset(actor));
                else if (actor.Models.Count == 1)
                    iconAssets.Add(CreateAsset(actor));
                else if (actor.Models.Count == 0)
                    emptyAssets.Add(CreateAsset(actor));
            }

            assets = new List<AssetItem>();

            assets.AddRange(folderAssets);
            assets.AddRange(iconAssets);
            assets.AddRange(emptyAssets);
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

        private AssetItem CreateAsset(ObjectDatabaseEntry objEntry)
        {
            if(objEntry.Models.Count > 1)
            {
                return new LiveActorFolder($"{Runtime.ExecutableDir}\\Lib\\Images\\ActorThumbnails\\{objEntry.ActorCategory}\\{objEntry.ClassName}");
            }

            string arcName = objEntry.Models.FirstOrDefault();

            string icoPath = "Node";

            if(arcName != null)
            {
                string path = $"{Runtime.ExecutableDir}\\Lib\\Images\\ActorThumbnails\\{objEntry.ActorCategory}\\{arcName}.png";

                if (IconManager.HasIcon(path))
                    icoPath = path;
                else
                    Console.WriteLine("Missing Icon at path: " + path);
            }

            return new LiveActorAsset(objEntry.ClassName, objEntry)
            {
                Name = objEntry.ClassName,
                Icon = IconManager.GetTextureIcon(icoPath)
            };
        }
    }

    public class LiveActorAsset : AssetItem
    {
        public LiveActorAsset(string filePath, ObjectDatabaseEntry entry) : base(filePath)
        {
            DatabaseEntry = entry;
        }
        public ObjectDatabaseEntry DatabaseEntry { get; private set; }
        public string ActorCategory { get { return DatabaseEntry.ActorCategory; } }

    }

    public class LiveActorFolder : AssetFolder
    {
        public LiveActorFolder(string filePath) : base(filePath)
        {
        }
    }
}
