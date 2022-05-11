using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;
using CafeLibrary.Rendering;

namespace CafeLibrary.Rendering
{
    public class SMOShaderLoader
    {
        public static string GamePath => SampleMapEditor.PluginConfig.GamePath;

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

           return TryLoadPath($"{GamePath}\\ShaderData", archive);
        }

        private static BfshaLibrary.BfshaFile TryLoadPath(string folder, string fileName)
        {
            string outputPath = $"GlobalShaders\\{fileName}.bfsha";
            if (GlobalShaderCache.ShaderFiles.ContainsKey(outputPath))
                return (BfshaLibrary.BfshaFile)GlobalShaderCache.ShaderFiles[outputPath];

            //Load cached file to disk if exist
            if (System.IO.File.Exists(outputPath)) {
                var bfsha = new BfshaLibrary.BfshaFile(outputPath);
                GlobalShaderCache.ShaderFiles.Add(outputPath, bfsha);
                return bfsha;
            }

            Console.WriteLine($"TryLoadPath " + $"{folder}\\{fileName}.szs");

            //Load from game folder instead if not cached 
            if (File.Exists($"{folder}\\{fileName}.szs")) {
                if (!Directory.Exists("GlobalShaders"))
                    Directory.CreateDirectory("GlobalShaders");

                //Cache the file and save to disk
                var sarc = SARC_Parser.UnpackRamN(YAZ0.Decompress($"{folder}\\{fileName}.szs"));
                File.WriteAllBytes(outputPath, sarc.Files[$"{fileName}.bfsha"]);

                var bfsha = new BfshaLibrary.BfshaFile(outputPath);
                GlobalShaderCache.ShaderFiles.Add(outputPath, bfsha);
                return bfsha;
            }
            return null;
        }
    }
}
