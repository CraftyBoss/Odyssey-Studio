using CafeLibrary.ModelConversion;
using CafeLibrary.Rendering;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;

namespace RedStarLibrary.Extensions
{
    internal static class MiscExtensions
    {
        public static Dictionary<string, float> ToDict(this Vector3 vector) => new Dictionary<string, float>() { {"X", vector.X }, { "Y", vector.Y }, { "Z", vector.Z } };

        public static string TrimEnd(this string source, string value)
        {
            if (!source.EndsWith(value))
                return source;

            return source.Remove(source.LastIndexOf(value));
        }

        public static bool ExportModel(this BfresRender bfresRender, string outPath, string outFolderName)
        {
            outPath = Path.Combine(outPath, outFolderName);
            if (!Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);
            else // dont bother re-dumping if the model folder is already present
                return false; 

            var resFile = bfresRender.ResFile;

            if (resFile == null)
                throw new NullReferenceException();

            foreach (var model in resFile.Models)
            {
                var modelPath = Path.Combine(outPath, model.Key + ".dae");

                var scene = BfresModelExporter.FromGeneric(resFile, model.Value);
                IONET.IOManager.ExportScene(scene, modelPath, new IONET.ExportSettings() { });
            }

            foreach ((var texName, var arcTex) in bfresRender.Textures)
                arcTex.OriginalSource?.Export(Path.Combine(outPath, $"{texName}.png"), new TextureExportSettings());

            return true;
        }
    }
}
