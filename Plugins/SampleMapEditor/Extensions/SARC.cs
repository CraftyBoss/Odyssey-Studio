using ByamlExt.Byaml;
using CafeLibrary;
using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using static CafeLibrary.SARC;

namespace RedStarLibrary.Extensions
{
    internal static class SARCExtensions
    {
        /// <summary>
        /// Loads the Textures found in the Actor's Texture Archive if found in InitModel.byml
        /// </summary>
        /// <param name="modelARC"> Archive that the Actor uses for Initialization and Model data. </param>
        /// <returns> Dictionary containing all textures found within the Texture Archive. </returns>
        public static Dictionary<string, GenericRenderer.TextureView> GetTexArchive(this SARC modelARC)
        {

            ArchiveFileInfo modelInfo = modelARC.files.Find(e => e.FileName == "InitModel.byml");

            if (modelInfo != null)
            {
                BymlFileData initModelByml = ByamlFile.LoadN(modelInfo.FileData, false);

                if (initModelByml.RootNode != null)
                {
                    if (initModelByml.RootNode is Dictionary<string, dynamic>)
                    {
                        if (((Dictionary<string, dynamic>)initModelByml.RootNode).ContainsKey("TextureArc"))
                        {
                            string texArcName = initModelByml.RootNode["TextureArc"];

                            return ResourceManager.FindOrLoadTextureList(Path.Combine("ObjectData", $"{texArcName}.szs"));
                        }
                    }
                }
            }

            return null;
        }
        /// <summary>
        /// Gets a Stream of the actor's model accociated with the SARC
        /// </summary>
        /// <param name="modelArc">Actor Archive the model file can be found in.</param>
        /// <param name="modelName">Name of model file found in actor archive. Leave empty to use the Archives name.</param>
        /// <returns></returns>
        public static Stream GetModelStream(this SARC modelArc, string modelName = null)
        {

            if (modelName == null)
                modelName = modelArc.FileInfo.FileName;

            ArchiveFileInfo modelFile = modelArc.files.Find(e => e.FileName.Contains($"{modelName}.bfres"));

            if (modelFile != null)
                return modelFile.FileData;
            else
                return null;
        }

        public static Stream GetFileStream(this SARC arc, string fileName)
        {
            ArchiveFileInfo modelFile = arc.files.Find(e => e.FileName.Contains(fileName));

            if (modelFile != null)
                return modelFile.FileData;
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelArc"></param>
        /// <param name="initName"></param>
        /// <returns></returns>
        public static Stream GetInitFileStream(this SARC modelArc, string initName)
        {
            ArchiveFileInfo initFile = modelArc.files.Find(e => e.FileName.Contains($"{initName}.byml"));

            if (initFile != null)
                return initFile.FileData;
            else
                return null;
        }

        public static bool RenameFile(this SARC arc, ArchiveFileInfo file, string newName)
        {
            if (file.FileName != newName)
            {
                Console.WriteLine($"Renaming {file.FileName} to {newName}.");

                // remove previous file
                arc.DeleteFile(file);

                // rename info, add back to archive
                file.FileName = newName;
                arc.AddFile(file);

                return true;
            }

            return false;
        }

        public static void AddFile(this SARC arc, string name, Stream data)
        {
            arc.files.Add(new FileEntry()
            {
                FileData = data,
                FileName = name,
            });
        }
    }
}
