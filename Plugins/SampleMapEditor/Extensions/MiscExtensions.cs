using CafeLibrary.ModelConversion;
using CafeLibrary.Rendering;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public static string AddSpacesToText(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);

            if (!char.IsUpper(text[0]))
                newText.Append(char.ToUpper(text[0]));
            else
                newText.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                else if (char.IsDigit(text[i]) && !char.IsDigit(text[i - 1]) && text[i - 1] != ' ')
                    newText.Append(' ');

                newText.Append(text[i]);
            }
            return newText.ToString();
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
