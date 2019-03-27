using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    internal class Program
    {


        private static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine("Hello World!");



            var config = new IpConfig()
            {
                IpDbPath = "~/IP/qqwry.dat"
            };
            var ipSearch = new MyIpSearch(config);
            var ips = new[]
            {
                "72.51.27.51",
                "127.0.0.1",
                "58.246.74.34",
                "107.182.187.67",
                "10.204.15.2",
                "40.73.66.99",
                "132.232.156.237",
                "47.104.86.68",
                "112.65.61.101",
                "255.255.255.255",
                "0.0.0.0",
                "1.1.1.1",
                "255.255.255.0"//记录版本信息  可以自己做替换显示
            };
            foreach (var ip in ips)
            {
                var ipLocation = ipSearch.GetIpLocation(ip);
                Write(ipLocation);
            }
            Console.WriteLine("记录总数" + ipSearch.IpCount);
            Console.WriteLine("版本" + ipSearch.Version);

            Console.ReadKey();
        }

        private static void Write(IpLocation ipLocation)
        {
            Console.WriteLine($"ip：{ipLocation.Ip}，country：{ipLocation.Country}，area：{ipLocation.Area}");
        }

        private static void Write(string ip, IPLocation ipLocation)
        {
            Console.WriteLine($"ip：{ip}，country：{ipLocation.country}，area：{ipLocation.area}");
        }

        /// <summary>
        /// Maps a virtual path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "~/bin"</param>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
        public static string MapRootPath(string path)
        {
            path = path.Replace("~/", "").TrimStart('/').Replace('/', '\\');
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, path);
        }

        public static int GetInt32(byte[] buffer, int offset, bool littleEndian)
        {
            if (littleEndian)
            {
                return buffer[offset + 1] << 8 | buffer[offset];
            }
            else
            {
                return buffer[offset] << 8 | buffer[offset + 1];
            }
        }

        private static int ReadIntByByte(BinaryReader bReader)
        {
            byte[] b = new byte[4];
            for (int i = 3; i >= 0; i--)
            {
                b[i] = bReader.ReadByte();
            }
            return BitConverter.ToInt32(b, 0);
        }

    }

    ///<summary>
    /// 地理位置,包括国家和地区
    ///</summary>
    public struct IpLocation
    {
        public string Ip, Country, Area;
    }

    public class IpConfig
    {
        public string IpDbPath { get; set; }
    }

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
