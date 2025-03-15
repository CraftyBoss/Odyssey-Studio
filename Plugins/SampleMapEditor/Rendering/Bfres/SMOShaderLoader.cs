using System;
using System.IO;
using Toolbox.Core.IO;
using CafeLibrary.Rendering;
using CafeLibrary;

namespace RedStarLibrary.Rendering
{
    public class SMOShaderLoader
    {
        public static string GamePath => PluginConfig.GamePath;

        public static BfshaLibrary.BfshaFile LoadShader(string archive)
        {
            foreach (var file in GlobalShaderCache.ShaderFiles.Values) {
                if (file is BfshaLibrary.BfshaFile) {
                    if (((BfshaLibrary.BfshaFile)file).Name == archive)
                    {
                        return (BfshaLibrary.BfshaFile)file;
                    }
                }
            }

           return TryLoadPath(Path.Combine(GamePath, "ShaderData"), archive);
        }

        private static BfshaLibrary.BfshaFile TryLoadPath(string folder, string fileName)
        {
            string outputPath = Path.Combine("GlobalShaders", $"{fileName}.bfsha");
            if (GlobalShaderCache.ShaderFiles.ContainsKey(outputPath))
                return (BfshaLibrary.BfshaFile)GlobalShaderCache.ShaderFiles[outputPath];

            //Load cached file to disk if exist
            if (System.IO.File.Exists(outputPath)) {
                var bfsha = new BfshaLibrary.BfshaFile(outputPath);
                GlobalShaderCache.ShaderFiles.Add(outputPath, bfsha);
                return bfsha;
            }

            string loadPath = Path.Combine(folder, $"{fileName}.szs");
            Console.WriteLine($"TryLoadPath " + loadPath);

            //Load from game folder instead if not cached 
            if (File.Exists(loadPath)) {
                if (!Directory.Exists("GlobalShaders"))
                    Directory.CreateDirectory("GlobalShaders");

                //Cache the file and save to disk
                var sarc = SARC_Parser.UnpackRamN(YAZ0.Decompress(loadPath));
                File.WriteAllBytes(outputPath, sarc.Files[$"{fileName}.bfsha"]);

                var bfsha = new BfshaLibrary.BfshaFile(outputPath);
                GlobalShaderCache.ShaderFiles.Add(outputPath, bfsha);
                return bfsha;
            }
            return null;
        }
    }
}
