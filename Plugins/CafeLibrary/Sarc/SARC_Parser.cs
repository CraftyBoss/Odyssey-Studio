using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Toolbox.Core.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CafeLibrary
{
    public class SarcData
    {
        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
    }

    public static class SARC_Parser
    {
        //From https://github.com/aboood40091/SarcLib/
        public static string GuessFileExtension(byte[] f)
        {
            string Ext = ".bin";
            if (f.Matches("SARC")) Ext = ".sarc";
            else if (f.Matches("Yaz")) Ext = ".szs";
            else if (f.Matches("YB") || f.Matches("BY")) Ext = ".byaml";
            else if (f.Matches("FRES")) Ext = ".bfres";
            else if (f.Matches("Gfx2")) Ext = ".gtx";
            else if (f.Matches("FLYT")) Ext = ".bflyt";
            else if (f.Matches("CLAN")) Ext = ".bclan";
            else if (f.Matches("CLYT")) Ext = ".bclyt";
            else if (f.Matches("FLIM")) Ext = ".bclim";
            else if (f.Matches("FLAN")) Ext = ".bflan";
            else if (f.Matches("FSEQ")) Ext = ".bfseq";
            else if (f.Matches("VFXB")) Ext = ".pctl";
            else if (f.Matches("AAHS")) Ext = ".sharc";
            else if (f.Matches("BAHS")) Ext = ".sharcb";
            else if (f.Matches("BNTX")) Ext = ".bntx";
            else if (f.Matches("BNSH")) Ext = ".bnsh";
            else if (f.Matches("FSHA")) Ext = ".bfsha";
            else if (f.Matches("FFNT")) Ext = ".bffnt";
            else if (f.Matches("CFNT")) Ext = ".bcfnt";
            else if (f.Matches("CSTM")) Ext = ".bcstm";
            else if (f.Matches("FSTM")) Ext = ".bfstm";
            else if (f.Matches("STM")) Ext = ".bfsha";
            else if (f.Matches("CWAV")) Ext = ".bcwav";
            else if (f.Matches("FWAV")) Ext = ".bfwav";
            else if (f.Matches("CTPK")) Ext = ".ctpk";
            else if (f.Matches("CGFX")) Ext = ".bcres";
            else if (f.Matches("AAMP")) Ext = ".aamp";
            else if (f.Matches("MsgStdBn")) Ext = ".msbt";
            else if (f.Matches("MsgPrjBn")) Ext = ".msbp";
            else if (f.Matches((uint)(f.Length - 0x28), "FLIM")) Ext = ".bflim";
            return Ext;
        }

        public static uint GuessAlignment(Dictionary<string, byte[]> files)
        {
            uint res = 4;
            foreach (var f in files.Values)
            {
                uint fileRes = GuessFileAlignment(f);
                res = fileRes > res ? fileRes : res;
            }
            return res;
        }

        public static uint GuessFileAlignment(byte[] f)
        {
            if (f.Matches("SARC")) return 0x2000;
            else if (f.Matches("Yaz")) return 0x80;
            else if (f.Matches("YB") || f.Matches("BY")) return 0x80;
            else if (f.Matches("FRES") || f.Matches("Gfx2") || f.Matches("AAHS") || f.Matches("BAHS")) return 0x2000;
            else if (f.Matches("EFTF") || f.Matches("VFXB") || f.Matches("SPBD")) return 0x2000;
            else if (f.Matches("BNTX") || f.Matches("BNSH") || f.Matches("FSHA")) return 0x1000;
            else if (f.Matches("FFNT")) return 0x2000;
            else if (f.Matches("CFNT")) return 0x80;
            else if (f.Matches(1, "STM") /* *STM */ || f.Matches(1, "WAV") || f.Matches("FSTP")) return 0x20;
            else if (f.Matches("CTPK")) return 0x10;
            else if (f.Matches("CGFX")) return 0x80;
            else if (f.Matches("AAMP")) return 8;
            else if (f.Matches("MsgStdBn") || f.Matches("MsgPrjBn")) return 0x80;
            else if (f.Matches((uint)(f.Length - 0x28), "FLIM")) return (uint)f.GetAlignment((uint)(f.Length - 8), typeof(ushort));
            else return 0x4;
        }

        public static Tuple<int, byte[]> PackN(SarcData data, int _align = -1)
        {
            int align = _align >= 0 ? _align : (int)GuessAlignment(data.Files);

            int nodeArraySize = Unsafe.SizeOf<SFATNode>() * data.Files.Count;

            int fileDataSize = 0;
            for (int i = 0; i < data.Files.Values.Count; i++)
            {
                var e = data.Files.Values.ElementAt(i);

                if (i == data.Files.Count - 1)
                    fileDataSize += e.Length;
                else
                    fileDataSize += alignUp(e.Length, (int)GuessFileAlignment(e));
            }

            int strTableSize = data.Files.Keys.Select(e => alignUp(Encoding.UTF8.GetByteCount(e) + 1, 0x4)).Sum();

            int totalHeaderSize = Unsafe.SizeOf<SARCHeader>() + Unsafe.SizeOf<SFATHeader>() + Unsafe.SizeOf<SFNTHeader>();
            int totalFileSize = fileDataSize + strTableSize + nodeArraySize + totalHeaderSize;

            byte[] outData = new byte[totalFileSize];

            Span<byte> serializedData = outData;
            int offset = 0;

            SARCHeader sarcHeader = new SARCHeader()
            {
                magic = SARCHeader.MagicValue,
                headerLength = (ushort)Unsafe.SizeOf<SARCHeader>(),
                BOM = SARCHeader.EndiannessValue,
                size = (uint)totalFileSize,
                dataStart = (uint)(strTableSize + nodeArraySize + totalHeaderSize),
                version = 0x100
            };
            MemoryMarshal.Write(serializedData[offset..], sarcHeader);
            offset += sarcHeader.headerLength;

            SFATHeader sfatHeader = new SFATHeader()
            {
                magic = SFATHeader.MagicValue,
                headerLength = (ushort)Unsafe.SizeOf<SFATHeader>(),
                nodeCount = (ushort)data.Files.Count,
                hashKey = SFATHeader.HashKeyValue
            };
            MemoryMarshal.Write(serializedData[offset..], sfatHeader);
            offset += sfatHeader.headerLength;

            uint curStrTableOffset = 0;
            uint curDataTableOffset = 0;

            int tempTableStart = offset + nodeArraySize + Unsafe.SizeOf<SFNTHeader>();
            int strTableStart = offset + nodeArraySize + Unsafe.SizeOf<SFNTHeader>();
            int dataOffset = (int)sarcHeader.dataStart;

            foreach (var dataEntry in data.Files)
            {
                SFATNode node = new SFATNode()
                {
                    fileNameHash = SFATNode.GetHash(dataEntry.Key, sfatHeader.hashKey),
                    fileAttr = ((curStrTableOffset / 4)) | (uint)SFATAttributes.HasNameTableOffset,
                    dataStart = curDataTableOffset,
                    dataEnd = curDataTableOffset + (uint)dataEntry.Value.Length,
                };

                MemoryMarshal.Write(serializedData[offset..], node);
                offset += Unsafe.SizeOf<SFATNode>();

                Encoding.UTF8.GetBytes(dataEntry.Key).CopyTo(serializedData[strTableStart..]);
                strTableStart += alignUp(Encoding.UTF8.GetByteCount(dataEntry.Key) + 1, 0x4);

                dataEntry.Value.CopyTo(serializedData[dataOffset..]);
                dataOffset += alignUp(dataEntry.Value.Length, (int)GuessFileAlignment(dataEntry.Value));

                uint byteCount = (uint)(alignUp(Encoding.UTF8.GetByteCount(dataEntry.Key) + 1, 0x4));
                curStrTableOffset += byteCount;

                curDataTableOffset += (uint)alignUp(dataEntry.Value.Length, (int)GuessFileAlignment(dataEntry.Value));
            }

            SFNTHeader sfntHeader = new SFNTHeader()
            {
                magic = SFNTHeader.MagicValue,
                headerLength = (ushort)Unsafe.SizeOf<SFNTHeader>()
            };
            MemoryMarshal.Write(serializedData[offset..], sfntHeader);
            offset += sfntHeader.headerLength;

            return new Tuple<int, byte[]>(align, outData);
        }

        public static SarcData UnpackRamN(ReadOnlySpan<byte> data)
        {
            int offset = 0;
            SARCHeader header = MemoryMarshal.Read<SARCHeader>(data[offset..]);
            offset += header.headerLength;

            SFATHeader sfatHeader = MemoryMarshal.Read<SFATHeader>(data[offset..]);
            offset += sfatHeader.headerLength;

            int arrayLength = Unsafe.SizeOf<SFATNode>() * sfatHeader.nodeCount;
            ReadOnlySpan<SFATNode> nodes = MemoryMarshal.Cast<byte, SFATNode>(data[offset..(offset + arrayLength)]);
            offset += arrayLength;

            SFNTHeader sfntHeader = MemoryMarshal.Read<SFNTHeader>(data[offset..]);
            offset += sfntHeader.headerLength;

            ReadOnlySpan<byte> stringTable = data[offset..];
            ReadOnlySpan<byte> fileData = data[(int)header.dataStart..];

            SarcData sarcData = new SarcData();

            foreach (SFATNode node in nodes)
            {
                if (!((SFATAttributes)node.fileAttr).HasFlag(SFATAttributes.HasNameTableOffset))
                    throw new Exception("Node does not have a name!");

                int stringStart = ((int)node.fileAttr & 0xFFFF) * 4;
                int stringEnd = stringStart + stringTable[stringStart..].IndexOf((byte)0);

                string fileName = Encoding.UTF8.GetString(stringTable[stringStart..stringEnd]);

                if (string.IsNullOrWhiteSpace(fileName))
                    Console.WriteLine("SFATNode referenced an empty string!");

                ReadOnlySpan<byte> nodeData = fileData[(int)node.dataStart..(int)node.dataEnd];

                sarcData.Files.Add(fileName, nodeData.ToArray());
            }

            return sarcData;
        }

        public static SarcData UnpackRamN(Stream src) => UnpackRamN(src.ToArray());

        private static int alignUp(int value, int alignment)
        {
            int mask = alignment - 1;
            return (value + mask) & ~mask;
        }
    }
}
