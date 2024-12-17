﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using FolderBrowserEx;
using Syroot.NintenTools.NSW.Bfres;
using Toolbox.Core.IO;
using Toolbox.Core;
using ZstdSharp;
using ZstdSharp.Unsafe;
using MapStudio.UI;

namespace CafeLibrary
{
    internal class MeshCodec
    {
        static ResFile ExternalStringBinary;

        public static ExternalFlags GetExternalFlags(Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                using (reader.TemporarySeek(10, SeekOrigin.Begin))
                {
                    byte version = reader.ReadByte();
                    //Check if bfres supports external strings or not
                    if (version < 10)
                        return (ExternalFlags)0;

                    //Check external flags
                    reader.SeekBegin(0xee);
                    ExternalFlags flag = (ExternalFlags)reader.ReadByte();
                    byte flag2 = reader.ReadByte();
                    if (flag2 == 1) //flag custom set by bfres resave to detect an mc resave
                        return ExternalFlags.MeshCodecResave;
                    return flag;
                }
            }
        }

        //Flags thanks to watertoon
        public enum ExternalFlags : byte
        {
            IsExternalModelUninitalized = 1 << 0,
            HasExternalString = 1 << 1,
            HoldsExternalStrings = 1 << 2,
            HasExternalGPU = 1 << 3,

            MeshCodecResave = 1 << 7,
        }

        public static void Prepare()
        {
            //Check if a valid directory exists
            string path = Path.Combine(PluginConfig.TotkGamePath, "Shader", "ExternalBinaryString.bfres.mc");
            if (!File.Exists(path))
            {
                TinyFileDialog.MessageBoxErrorOk("A game dump of TOTK is required to load this file. Please select the romfs folder path.");

                ImguiFolderDialog dlg = new ImguiFolderDialog();
                if (dlg.ShowDialog())
                {
                    if (!File.Exists(Path.Combine(dlg.SelectedPath, "Shader", "ExternalBinaryString.bfres.mc")))
                        return;

                    PluginConfig.TotkGamePath = dlg.SelectedPath;

                    ZDic.DumpExternalDictionaries();
                    PluginConfig.Instance.Save();
                }
            }
            LoadExternalStrings();
        }

        public static void LoadExternalStrings()
        {
            if (ExternalStringBinary != null)
                return;

            string path = Path.Combine(PluginConfig.TotkGamePath, "Shader", "ExternalBinaryString.bfres.mc");
            byte[] data = DecompressMeshCodec(path);
            //Load string table into memory
            //Strings are stored in a static list which will be used for opened bfres
            ExternalStringBinary = new ResFile(new MemoryStream(data));
        }

        public static byte[] DecompressMeshCodec(string file)
        {
            using (var fs = File.OpenRead(file))
            {
                return DecompressMeshCodec(fs);
            }
        }

        public static byte[] DecompressMeshCodec(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadUInt32(); //Magic
                reader.ReadUInt32(); //Version 1.1.0.0
                var flags = reader.ReadInt32();
                var decompressed_size = (flags >> 5) << (flags & 0xf);

                reader.BaseStream.Seek(0xC, SeekOrigin.Begin);
                byte[] src = reader.ReadBytes((int)reader.BaseStream.Length - 0xC);
                return Decompress(src, (uint)decompressed_size);
            }
        }

        static unsafe byte[] Decompress(byte[] src, uint decompressed_size)
        {
            var dctx = Methods.ZSTD_createDCtx();
            Methods.ZSTD_DCtx_setFormat(dctx, ZSTD_format_e.ZSTD_f_zstd1_magicless);
            var uncompressed = new byte[decompressed_size];
            fixed (byte* srcPtr = src)
            fixed (byte* uncompressedPtr = uncompressed)
            {
                var decompressedLength = Methods.ZSTD_decompressDCtx(dctx, uncompressedPtr, (UIntPtr)uncompressed.Length, srcPtr, (UIntPtr)src.Length);

                byte[] arr = new byte[(uint)decompressed_size];
                Marshal.Copy((IntPtr)uncompressedPtr, arr, 0, arr.Length);
                return arr;
            }
        }

        public static byte[] CompressMeshCodec(Stream stream)
        {
            var src = stream.ToArray();

            var mem = new MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write(1263551309); //MCPK
                //Version 1.1.0.0
                writer.Write((byte)1);
                writer.Write((byte)1);
                writer.Write((byte)0);
                writer.Write((byte)0);
                //Flags
                writer.Write(GetMeshCodecFlags((uint)src.Length));
                //ZSTD bfres with no magic
                writer.Write(CompressZSTD(src));
            }
            return mem.ToArray();
        }

        static uint GetMeshCodecFlags(uint decompSize)
        {
            var aligned = (uint)(-decompSize % 0x1000 + 0x1000) % 0x1000;
            decompSize = decompSize + aligned;
            return ((decompSize >> (int)0xc) << 5) + 0xc;
        }

        static byte[] CompressZSTD(byte[] src)
        {
            int level = 20;
            Compressor comp = new Compressor(level);
            comp.SetParameter(ZSTD_cParameter.ZSTD_c_contentSizeFlag, 0);
            comp.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 0);
            comp.SetParameter(ZSTD_cParameter.ZSTD_c_dictIDFlag, 0);
            comp.SetParameter(ZSTD_cParameter.ZSTD_c_experimentalParam2, 1);

            var compressed = comp.Wrap(src);
            return compressed.ToArray();
        }

        public void Dispose()
        {
        }
    }
}