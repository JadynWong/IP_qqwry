using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using QQWry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace QQWry
{
    public class QQWryIpSearch : IDisposable, IIpSearch
    {
        public const string CopywriteUrl = "http://update.cz88.net/ip/copywrite.rar";
        public const string QqwryUrl = "http://update.cz88.net/ip/qqwry.rar";

        /// <summary>
        /// 初始化锁定对象
        /// </summary>
        private static readonly object _lockInit = new object();
        /// <summary>
        /// 读取锁定对象
        /// </summary>
        private readonly object _lockRead = new object();
        /// <summary>
        /// 内存数据库
        /// </summary>
        private MemoryStream QQWryDBFile;
        /// <summary>
        /// Ip索引
        /// </summary>
        private long[] IpArrayIndex;
        /// <summary>
        /// 起始定位
        /// </summary>
        private long StartPosition;
        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool? Inited;
        /// <summary>
        /// IP地址正则验证
        /// </summary>
        private static Regex IpAddressRegex => new Regex(@"(\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b)");

        private readonly QQWryOptions _qqwryOptions;
        private int? ipCount;
        private string _version;

        /// <summary>
        /// 记录总数
        /// </summary>
        public int IpCount
        {
            get
            {
                if (!ipCount.HasValue)
                {
                    Init();
                    ipCount = IpArrayIndex.Length;
                }

                return ipCount.Value;
            }
        }

        /// <summary>
        /// 版本信息
        /// </summary>
        public string Version
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_version))
                {
                    _version = GetIpLocation("255.255.255.255").Area;
                }
                return _version;
            }
        }

        public QQWryIpSearch(QQWryOptions options)
        {
            _qqwryOptions = options;
        }

        #region public Method

        /// <summary>
        /// 更新数据库
        /// </summary>
        public void UpdateDb()
        {
            var client = new HttpClient();
            var copywrite = client.GetStreamAsync(CopywriteUrl).Result;
            var qqwry = client.GetByteArrayAsync(QqwryUrl).Result;
            if (copywrite == null)
            {
                throw new Exception("-1 copywrite can't null");
            }
            var binaryReader = new BinaryReader(copywrite);
            var sign = Encoding.GetEncoding("gb2312").GetString(binaryReader.ReadBytesLE(4).Where(x => x != 0x00).ToArray());
            var version = binaryReader.ReadUInt32LE();
            var unknown1 = binaryReader.ReadUInt32LE();
            var size = binaryReader.ReadUInt32LE();
            var unknown2 = binaryReader.ReadUInt32LE();
            var key = binaryReader.ReadUInt32LE();
            var text = Encoding.GetEncoding("gb2312").GetString(binaryReader.ReadBytesLE(128).Where(x => x != 0x00).ToArray());
            var link = Encoding.GetEncoding("gb2312").GetString(binaryReader.ReadBytesLE(128).Where(x => x != 0x00).ToArray());

            //extract information from copywrite.rar
            if (qqwry.Length <= 24 || sign != "CZIP")
            {
                throw new Exception("-2 sign error");
            }

            if (qqwry.Length != size)
            {
                throw new Exception("-4 size error");
            }
            //decrypt
            var head = new byte[0x200];
            for (var i = 0; i < 0x200; i++)
            {
                key = (key * 0x805 + 1) & 0xff;
                head[i] = (byte)(qqwry[i] ^ key);
            }
            Array.Copy(head, 0, qqwry, 0, head.Length);
            var dataBuffer = new byte[4096];

            //decompress
            using (var inflaterInputStream = new InflaterInputStream(new MemoryStream(qqwry)))
            {
                //write file
                using (var fsOut = File.Create(_qqwryOptions.DbPath))
                {
                    //inflaterInputStream.CopyTo(fsOut);
                    StreamUtils.Copy(inflaterInputStream, fsOut, dataBuffer);
                }
            }
        }

        public bool Init()
        {
            if (Inited != null)
            {
                return Inited.Value;
            }

            lock (_lockInit)
            {
                if (Inited != null)
                {
                    return Inited.Value;
                }

                Inited = false;

                var ipDbPath = _qqwryOptions.DbPath;
                var dir = Path.GetDirectoryName(ipDbPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!File.Exists(ipDbPath))
                {
                    Console.WriteLine("无法找到IP数据库{0}", ipDbPath);
                    UpdateDb();
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("使用IP数据库{0}", ipDbPath);
#endif
                using (var fs = File.OpenRead(ipDbPath))
                {
                    try
                    {
                        QQWryDBFile = new MemoryStream();
                        fs.CopyTo(QQWryDBFile);
                        QQWryDBFile.Position = 0;
                        IpArrayIndex = BlockToArray(ReadIpBlock(QQWryDBFile, out StartPosition));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        throw ex;
                    }
                }
            }

            if (QQWryDBFile == null)
            {
                throw new InvalidOperationException("无法打开IP数据库" + _qqwryOptions.DbPath + "！");
            }

            Inited = true;
            return true;
        }

        /// <summary>
        /// 检查IP合法性
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool CheckIp(string ip)
        {
            return IpAddressRegex.IsMatch(ip);
            //if (string.IsNullOrWhiteSpace(ip))
            //{
            //    return false;
            //}
            //var arr = ip.Split('.');

            //if (arr.Length != 4)
            //{
            //    return false;
            //}
            //else
            //{
            //    for (var i = 0; i < 4; i++)
            //    {
            //        if (int.TryParse(arr[i], out var part))
            //        {
            //            if (part < 0 || part > 255)
            //            {
            //                return false;
            //            }
            //        }
            //    }
            //    return true;
            //}
        }

        ///<summary>
        /// 获取指定IP所在地理位置
        ///</summary>
        ///<param name="strIp">要查询的IP地址</param>
        ///<returns></returns>
        public IpLocation GetIpLocation(string strIp)
        {
            var loc = new IpLocation
            {
                Ip = strIp
            };
            if (!CheckIp(strIp) || !Init())
            {
                return loc;
            }
            long ip = IpToLong(strIp);
            if ((ip >= IpToLong("127.0.0.1") && (ip <= IpToLong("127.255.255.255"))))
            {
                loc.Country = "本机内部环回地址";
                loc.Area = string.Empty;
                return loc;
            }
            if (!Init())
            {
                return loc;
            }
            long offset = SearchIp(ip, IpArrayIndex, 0, IpArrayIndex.Length) * 7 + 4;
            lock (_lockRead)
            {
                QQWryDBFile.Position = StartPosition;
                //跳过起始IP
                QQWryDBFile.Position += offset;
                //跳过结束IP
                QQWryDBFile.Position = ReadLongX(QQWryDBFile, 3) + 4;

                //读取标志
                var flag = QQWryDBFile.ReadByte();
                //表示国家和地区被转向
                if (flag == 1)
                {
                    QQWryDBFile.Position = ReadLongX(QQWryDBFile, 3);
                    //再读标志
                    flag = QQWryDBFile.ReadByte();
                }
                var countryOffset = QQWryDBFile.Position;
                loc.Country = ReadString(QQWryDBFile, flag);

                if (flag == 2)
                {
                    QQWryDBFile.Position = countryOffset + 3;
                }
                flag = QQWryDBFile.ReadByte();
                loc.Area = ReadString(QQWryDBFile, flag);
                if (" CZ88.NET".Equals(loc.Area, StringComparison.CurrentCultureIgnoreCase))
                {
                    loc.Area = string.Empty;
                }
                return loc;
            }

        }

        public void Dispose()
        {
            QQWryDBFile?.Dispose();
            QQWryDBFile = null;
            IpArrayIndex = null;
            Inited = null;
        }

        #endregion

        #region static Method

        ///<summary>
        /// 将字符串形式的IP转换位long
        ///</summary>
        ///<param name="strIp"></param>
        ///<returns></returns>
        private static long IpToLong(string strIp)
        {
            var ipBytes = new byte[8];
            var strArr = strIp.Split(new char[] { '.' });
            for (var i = 0; i < 4; i++)
            {
                ipBytes[i] = byte.Parse(strArr[3 - i]);
            }
            return BitConverter.ToInt64(ipBytes, 0);
        }

        ///<summary>
        /// 将索引区字节块中的起始IP转换成Long数组
        ///</summary>
        ///<param name="ipBlock"></param>
        private static long[] BlockToArray(byte[] ipBlock)
        {
            var ipArray = new long[ipBlock.Length / 7];
            var ipIndex = 0;
            var temp = new byte[8];
            for (var i = 0; i < ipBlock.Length; i += 7)
            {
                Array.Copy(ipBlock, i, temp, 0, 4);
                ipArray[ipIndex] = BitConverter.ToInt64(temp, 0);
                ipIndex++;
            }
            return ipArray;
        }

        /// <summary>
        ///  从IP数组中搜索指定IP并返回其索引
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="ipArray">IP数组</param>
        /// <param name="start">指定搜索的起始位置</param>
        /// <param name="end">指定搜索的结束位置</param>
        /// <returns></returns>
        private static int SearchIp(long ip, long[] ipArray, int start, int end)
        {
            while (true)
            {
                //计算中间索引
                var middle = (start + end) / 2;
                if (middle == start)
                {
                    return middle;
                }
                else if (ip < ipArray[middle])
                {
                    end = middle;
                }
                else
                {
                    start = middle;
                }
            }
        }

        ///<summary>
        /// 读取IP文件中索引区块
        ///</summary>
        ///<returns></returns>
        private static byte[] ReadIpBlock(Stream stream, out long startPosition)
        {
            startPosition = ReadLongX(stream, 4);
            var endPosition = ReadLongX(stream, 4);
            var count = (endPosition - startPosition) / 7 + 1;//总记录数
            stream.Position = startPosition;
            var ipBlock = new byte[count * 7];
            stream.Read(ipBlock, 0, ipBlock.Length);
            stream.Position = startPosition;
            return ipBlock;
        }

        /// <summary>
        ///  从IP文件中读取指定字节并转换位long
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytesCount">需要转换的字节数，主意不要超过8字节</param>
        /// <returns></returns>
        private static long ReadLongX(Stream stream, int bytesCount)
        {
            var bytes = new byte[8];
            stream.Read(bytes, 0, bytesCount);
            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>
        ///  从IP文件中读取字符串
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="flag">转向标志</param>
        /// <returns></returns>
        private static string ReadString(Stream stream, int flag)
        {
            if (flag == 1 || flag == 2)//转向标志
            {
                stream.Position = ReadLongX(stream, 3);
            }
            else
            {
                stream.Position -= 1;
            }

            var list = new List<byte>();
            var b = (byte)stream.ReadByte();
            while (b > 0)
            {
                list.Add(b);
                b = (byte)stream.ReadByte();
            }
            return Encoding.GetEncoding("GB2312").GetString(list.ToArray());
        }

        #endregion
    }
}
