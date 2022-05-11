using CafeLibrary;
using CafeLibrary.Rendering;
using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace RedStarLibrary
{
    class ResourceManager
    {

        private static Dictionary<string, SARC> LoadedSARCS = new Dictionary<string, SARC>();
        private static Dictionary<string, Dictionary<string, GenericRenderer.TextureView>> LoadedTextures = new Dictionary<string, Dictionary<string, GenericRenderer.TextureView>>();
        public static void ClearTextureList()
        {
            LoadedTextures = new Dictionary<string, Dictionary<string, GenericRenderer.TextureView>>();
        }
        public static Dictionary<string, GenericRenderer.TextureView> FindOrLoadTextureList(string arcPath)
        {

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
            string arcName = Path.GetFileNameWithoutExtension(sarcPath);

            if (!LoadedSARCS.ContainsKey(arcName))
            {
                SARC arc = new SARC();

                arc.Load(new MemoryStream(YAZ0.Decompress(sarcPath)));

                LoadedSARCS.Add(arcName, arc);
            }

            return LoadedSARCS[arcName];
        }
    }
}
