using ImGuiNET;
using MapStudio.UI;
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

        public AssetLoaderLiveActor(string category) : base()
        {
            this.category = category;
        }

        public List<AssetItem> Reload()
        {
            List<AssetItem> assets = new List<AssetItem>();

            foreach (var actor in ActorDataBase.GetDataBase().Where(e=> e.ActorCategory == category))
            {
                assets.Add(CreateAsset(actor));
            }

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
    }

    public class LiveActorFolder : AssetFolder
    {
        public LiveActorFolder(string filePath) : base(filePath)
        {
        }
    }
}
