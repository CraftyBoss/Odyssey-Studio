﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace CafeLibrary
{
    public class MeshCodecFormat : ICompressionFormat
    {
        public string[] Description { get; set; } = new string[] { "Mesh Codec Compression" };
        public string[] Extension { get; set; } = new string[] { "*.mc" };

        public override string ToString() => "Mesh Codec Compression";

        public bool Identify(Stream stream, string fileName)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "MCPK");
            }
        }

        public bool CanCompress { get; } = true;

        public Stream Decompress(Stream stream)
        {
            return new MemoryStream(MeshCodec.DecompressMeshCodec(stream));
        }

        public Stream Compress(Stream stream)
        {
            return new MemoryStream(MeshCodec.CompressMeshCodec(stream));
        }
    }
}
