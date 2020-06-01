using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QQWry
{
    public static class BinaryReaderExtensions
    {
        // Note this MODIFIES THE GIVEN ARRAY then returns a reference to the modified array.
        public static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        /// <summary>
        /// UInt16 BigEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static ushort ReadUInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt16(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(ushort)).Reverse() :
                binRdr.ReadBytesRequired(sizeof(ushort)), 0);
        }

        /// <summary>
        /// Int16 BigEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static short ReadInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt16(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(short)).Reverse() :
                binRdr.ReadBytesRequired(sizeof(ushort)), 0);
        }

        /// <summary>
        /// UInt32 BigEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static uint ReadUInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt32(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(uint)).Reverse() :
                binRdr.ReadBytesRequired(sizeof(uint)), 0);
        }

        /// <summary>
        /// Int32 BigEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static int ReadInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt32(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(int)).Reverse() :
                binRdr.ReadBytesRequired(sizeof(int)), 0);
        }

        /// <summary>
        /// Bytes BigEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        public static byte[] ReadBytesBE(this BinaryReader binRdr, int byteCount)
        {
            return BitConverter.IsLittleEndian
                ? binRdr.ReadBytesRequired(byteCount).ToArray()
                : binRdr.ReadBytesRequired(byteCount).Reverse().ToArray();
        }

        /// <summary>
        /// UInt16 LittleEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static ushort ReadUInt16LE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt16(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(ushort)) :
                binRdr.ReadBytesRequired(sizeof(ushort)).Reverse(), 0);
        }

        /// <summary>
        /// Int16 LittleEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static short ReadInt16LE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt16(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(short)) :
                binRdr.ReadBytesRequired(sizeof(ushort)).Reverse(), 0);
        }

        /// <summary>
        /// UInt32 LittleEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static uint ReadUInt32LE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt32(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(uint)) :
                binRdr.ReadBytesRequired(sizeof(uint)).Reverse(), 0);
        }

        /// <summary>
        /// Int32 LittleEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <returns></returns>
        public static int ReadInt32LE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt32(BitConverter.IsLittleEndian ?
                binRdr.ReadBytesRequired(sizeof(int)) :
                binRdr.ReadBytesRequired(sizeof(int)).Reverse(), 0);
        }

        /// <summary>
        /// Bytes LittleEndian
        /// </summary>
        /// <param name="binRdr"></param>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        public static byte[] ReadBytesLE(this BinaryReader binRdr, int byteCount)
        {
            return BitConverter.IsLittleEndian
                ? binRdr.ReadBytesRequired(byteCount).ToArray()
                : binRdr.ReadBytesRequired(byteCount).Reverse().ToArray();
        }

        public static byte[] ReadBytesRequired(this BinaryReader binRdr, int byteCount)
        {
            var result = binRdr.ReadBytes(byteCount);

            if (result.Length != byteCount)
            {
                throw new EndOfStreamException(
                    $"{byteCount} bytes required from stream, but only {result.Length} returned.");
            }

            return result;
        }
    }
}
