using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CafeLibrary
{
    public struct SFNTHeader
    {
        public const uint MagicValue = 0x544E4653;
        public uint magic;
        public ushort headerLength;
        public ushort reserved;
    }
}
