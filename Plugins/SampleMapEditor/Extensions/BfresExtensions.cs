using CafeLibrary.Rendering;
using RedStarLibrary.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.Extensions
{
    internal static class BfresExtensions
    {
        public static List<string> GetUsedTextureNames(this BfresRender bfresRender)
        {
            List<string> result = new List<string>();

            foreach (BfresModelRender model in bfresRender.Models)
            {
                foreach (BfresMeshRender mesh in model.Meshes)
                {
                    if (mesh.MaterialAsset is not SMORenderer matAsset)
                        continue;

                    foreach (var texMap in matAsset.Material.TextureMaps)
                    {
                        if (!result.Contains(texMap.Name))
                            result.Add(texMap.Name);
                    }
                }
            }

            return result;
        }
    }
}
