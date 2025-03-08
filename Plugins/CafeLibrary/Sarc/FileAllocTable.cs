using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CafeLibrary
{
    public struct SFATHeader
    {
        public const uint MagicValue = 0x54414653; // 0x53464154;
        
        public const uint HashKeyValue = 0x00000065;

        public uint magic;
        public ushort headerLength;
        public ushort nodeCount;
        public uint hashKey;
    }

    public struct SFATNode
    {
        public uint fileNameHash;
        public uint fileAttr;
        public uint dataStart;
        public uint dataEnd;

        public static uint GetHash(string name, uint key)
        {
            int result = 0;
            Span<byte> bytes = stackalloc byte[Encoding.UTF8.GetByteCount(name)];
            Encoding.UTF8.GetBytes(name, bytes);

            for (int i = 0; i < bytes.Length; i++)
                result = ((sbyte)bytes[i]) + result * (int)key;

            return (uint)result;
        }
    }

    [Flags]
    public enum SFATAttributes : uint
    {
        HasNameTableOffset = 0x01000000
    }
}
