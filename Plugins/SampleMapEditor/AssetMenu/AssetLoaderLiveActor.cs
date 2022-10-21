using ImGuiNET;
using MapStudio.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.AssetMenu
{
    public class AssetLoaderLiveActor : IAssetLoader
    {
        public string Name => "Stage Objects";

        public bool IsFilterMode => isFilter;
        private static bool isFilter = false;

        public List<AssetItem> Reload()
        {
            throw new NotImplementedException();
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
        public LiveActorAsset(string filePath) : base(filePath)
        {
        }
    }
}
