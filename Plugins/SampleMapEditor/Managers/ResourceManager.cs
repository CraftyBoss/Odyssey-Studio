using CafeLibrary;
using CafeLibrary.Rendering;
using GLFrameworkEngine;
using RedStarLibrary.Helpers;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace RedStarLibrary
{
    public class ResourceManager
    {

        private static Dictionary<string, SARC> LoadedSARCS = new Dictionary<string, SARC>();
        private static Dictionary<string, Dictionary<string, GenericRenderer.TextureView>> LoadedTextures = new Dictionary<string, Dictionary<string, GenericRenderer.TextureView>>();
        private static Dictionary<string, IRenderableTexture> TextureCache = new Dictionary<string, IRenderableTexture>();
        public static void ClearTextureList()
        {
            LoadedTextures = new Dictionary<string, Dictionary<string, GenericRenderer.TextureView>>();
        }

        public static Dictionary<string, GenericRenderer.TextureView> FindOrLoadTextureList(string relativePath)
        {

            string arcPath = FindResourcePath(relativePath);

            if (arcPath == null) return null;


            string texListName = Path.GetFileNameWithoutExtension(arcPath);

            if (!LoadedTextures.ContainsKey(texListName))
            {
                if (File.Exists(arcPath))
                {
                    SARC textureArc = new SARC();

                    textureArc.Load(new MemoryStream(YAZ0.Decompress(arcPath)));

                    ArchiveFileInfo texArcFile = textureArc.files.Find(e => e.FileName.Contains($"{texListName}.bfres"));

                    LoadedTextures.Add(texListName, BfresLoader.GetTextures(texArcFile.FileData));

                    return LoadedTextures[texListName];

                }

            }else
            {
                return LoadedTextures[texListName];
            }

            return null;
        }

        public static SARC FindOrLoadSARC(string sarcPath)
        {
            if (!File.Exists(sarcPath))
                sarcPath = FindResourcePath(sarcPath);

            if (!string.IsNullOrWhiteSpace(sarcPath) && File.Exists(sarcPath))
            {
                string arcName = Path.GetFileNameWithoutExtension(sarcPath);

                if (!LoadedSARCS.ContainsKey(arcName))
                {
                    SARC arc = new SARC();
                    arc.Load(new MemoryStream(YAZ0.Decompress(sarcPath)));
                    LoadedSARCS.Add(arcName, arc);
                }

                return LoadedSARCS[arcName];
            }
            else
                return null;
        }

        public static IRenderableTexture FindOrLoadRenderTex(STGenericTexture tex)
        {
            if (!TextureCache.TryGetValue(tex.Name, out var texture))
                TextureCache.Add(tex.Name, texture = GLTexture.FromGenericTexture(tex, tex.Parameters));

            return texture;
        }

        public static void ClearResources()
        {
            LoadedSARCS.Clear();
            LoadedTextures.Clear();
            TextureCache.Clear();
        }

        public static string FindResourcePath(string relativePath)
        {
            if (File.Exists(Path.Combine(WorkspaceHelper.WorkingDirectory, relativePath)))
                return Path.Combine(WorkspaceHelper.WorkingDirectory, relativePath);
            if (File.Exists(Path.Combine(PluginConfig.ModPath, relativePath)))
                return Path.Combine(PluginConfig.ModPath, relativePath);
            else if (File.Exists(Path.Combine(PluginConfig.GamePath, relativePath)))
                return Path.Combine(PluginConfig.GamePath, relativePath);
            else
                return null;
        }

        public static string FindResourceDirectory(string relativePath, bool isGamePathOnly = false)
        {
            if(isGamePathOnly)
            {
                if(Directory.Exists(Path.Combine(PluginConfig.GamePath, relativePath)))
                    return Path.Combine(PluginConfig.GamePath, relativePath);
                else
                    return null;
            }

            if (Directory.Exists(Path.Combine(WorkspaceHelper.WorkingDirectory, relativePath)))
                return Path.Combine(WorkspaceHelper.WorkingDirectory, relativePath);
            if (Directory.Exists(Path.Combine(PluginConfig.ModPath, relativePath)))
                return Path.Combine(PluginConfig.ModPath, relativePath);
            else if (Directory.Exists(Path.Combine(PluginConfig.GamePath, relativePath)))
                return Path.Combine(PluginConfig.GamePath, relativePath);
            else
                return null;
        }
    }
}
