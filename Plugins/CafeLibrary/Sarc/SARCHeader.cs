using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CafeLibrary
{
    public struct SARCHeader
    {
        public const uint MagicValue = 0x43524153;
        public const ushort EndiannessValue = 0xFEFF;

        public uint magic;
        public ushort headerLength;
        public ushort BOM;
        public uint size;
        public uint dataStart;
        public ushort version;
        public ushort reserved;
    }
}
