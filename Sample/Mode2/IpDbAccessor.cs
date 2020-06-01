using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QQWry
{
    internal class IpDbAccessor
    {
        private byte[] InnerBytes;

        public long Position { get; set; }

        public IpDbAccessor(byte[] bytes)
        {
            InnerBytes = bytes;
        }

        public byte ReadByte()
        {
            var ret = InnerBytes[Position];
            Position++;
            return ret;
        }

        public void Read(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                buffer[offset + i] = ReadByte();
            }
        }
    }
}
