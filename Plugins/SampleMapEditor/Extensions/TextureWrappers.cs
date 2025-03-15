using CafeLibrary;
using MapStudio.UI;
using System.IO;
using Toolbox.Core;

namespace RedStarLibrary.Extensions
{
    internal static class TextureFolderExtensions
    {
        public static void ExportAllTextures(this TextureFolder folder, string path, bool isOpenFolder, string ext = ".png")
        {
            foreach (var tex in folder.Children)
            {
                var texData = tex.Tag as STGenericTexture;
                texData.Export(Path.Combine(path, $"{tex.Header}{ext}"), new TextureExportSettings());
            }

            if(isOpenFolder)
                FileUtility.OpenFolder(path);
        }
    }
}
