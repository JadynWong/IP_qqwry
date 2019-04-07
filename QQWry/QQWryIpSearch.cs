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
using System.Threading;
using System.Threading.Tasks;

namespace QQWry
{
    /// <summary>
    /// QQWryIpSearch 请作为单例使用 数据库缓存在内存
    /// </summary>
    public class QQWryIpSearch : IDisposable, IIpSearch
    {
        //public const string CopyWriteUrl = "http://update.cz88.net/ip/copywrite.rar";
        //public const string QQWryUrl = "http://update.cz88.net/ip/qqwry.rar";

        ///// <summary>
        ///// 初始化锁定对象
        ///// </summary>
        //private static readonly object _lockInit = new object();
        ///// <summary>
        ///// 读取锁定对象
        ///// </summary>
        //private readonly object _lockRead = new object();

        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private object _versionLock = new object();

        /// <summary>
        /// 数据库 缓存
        /// </summary>
        private byte[] _qqwryDBFileBytes;
        /// <summary>
        /// Ip索引 缓存
        /// </summary>
        private long[] _ipIndexCache;
        /// <summary>
        /// 起始定位
        /// </summary>
        private long _startPosition;
        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool? _inited;
        /// <summary>
        /// IP地址正则验证
        /// </summary>
        private static Regex IpAddressRegex => new Regex(@"(\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b)");

        private static HttpClient _httpClient;

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
                    ipCount = _ipIndexCache.Length;
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
                if (!string.IsNullOrWhiteSpace(_version))
                {
                    return _version;
                }
                lock (_versionLock)
                {
                    if (!string.IsNullOrWhiteSpace(_version))
                    {
                        return _version;
                    }
                    _version = GetIpLocation("255.255.255.255").Area;
                    return _version;
                }
            }
        }

        static QQWryIpSearch()
        {
#if NETSTANDARD2_0

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            _httpClient = new HttpClient();
        }

        public QQWryIpSearch(QQWryOptions options)
        {
            _qqwryOptions = options;
        }

        #region public Method

        #region sync

        /// <summary>
        /// 更新数据库
        /// </summary>
        public void UpdateDb()
        {
            var copywrite = _httpClient.GetStreamAsync(_qqwryOptions.CopyWriteUrl).Result;
            var qqwry = _httpClient.GetByteArrayAsync(_qqwryOptions.QQWryUrl).Result;
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
            if (_inited != null)
            {
                return _inited.Value;
            }
            _initLock.Wait();
            try
            {
                if (_inited != null)
                {
                    return _inited.Value;
                }

                //_inited = false;

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
                System.Diagnostics.Debug.WriteLine($"使用IP数据库{ipDbPath}");
#endif
                if (_qqwryDBFileBytes != null)
                {
                    _qqwryDBFileBytes = null;
                }
                _qqwryDBFileBytes = FileToBytes(ipDbPath);
                using (var stream = new MemoryStream(_qqwryDBFileBytes))
                {
                    _ipIndexCache = BlockToArray(ReadIpBlock(stream, out _startPosition));
                }

            }
            finally
            {
                _initLock.Release();
            }

            if (_qqwryDBFileBytes == null)
            {
                throw new InvalidOperationException("无法打开IP数据库" + _qqwryOptions.DbPath + "！");
            }

            _inited = true;
            return true;

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
            if (!CheckIp(strIp))
            {
                return loc;
            }
            long ip = IpToLong(strIp);
            if (ip == IpToLong("127.0.0.1"))
            {
                loc.Country = "本机内部环回地址";
                loc.Area = string.Empty;
                return loc;
            }
            if (!Init())
            {
                return loc;
            }
            long offset = SearchIp(ip, _ipIndexCache, 0, _ipIndexCache.Length) * 7 + 4;

            using (var stream = new MemoryStream(_qqwryDBFileBytes))
            {
                stream.Position = _startPosition;
                //跳过起始IP
                stream.Position += offset;
                //跳过结束IP
                stream.Position = ReadLongX(stream, 3) + 4;

                //读取标志
                var flag = stream.ReadByte();
                //表示国家和地区被转向
                if (flag == 1)
                {
                    stream.Position = ReadLongX(stream, 3);
                    //再读标志
                    flag = stream.ReadByte();
                }
                var countryOffset = stream.Position;
                loc.Country = ReadString(stream, flag);

                if (flag == 2)
                {
                    stream.Position = countryOffset + 3;
                }
                flag = stream.ReadByte();
                loc.Area = ReadString(stream, flag);
            }
            if (" CZ88.NET".Equals(loc.Area, StringComparison.CurrentCultureIgnoreCase))
            {
                loc.Area = string.Empty;
            }
            return loc;

        }

        #endregion

        #region async
        /// <summary>
        /// 更新数据库
        /// </summary>
        public async Task UpdateDbAsync(CancellationToken token = default(CancellationToken))
        {
            var copywrite = await _httpClient.GetStreamAsync(_qqwryOptions.CopyWriteUrl).ConfigureAwait(false);
            var qqwry = await _httpClient.GetByteArrayAsync(_qqwryOptions.QQWryUrl).ConfigureAwait(false);
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

        public async Task<bool> InitAsync(CancellationToken token = default(CancellationToken))
        {
            if (_inited != null)
            {
                return _inited.Value;
            }
            await _initLock.WaitAsync(token);
            try
            {
                if (_inited != null)
                {
                    return _inited.Value;
                }

                var ipDbPath = _qqwryOptions.DbPath;
                var dir = Path.GetDirectoryName(ipDbPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!File.Exists(ipDbPath))
                {
                    Console.WriteLine("无法找到IP数据库{0}", ipDbPath);
                    await UpdateDbAsync();
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("使用IP数据库{0}", ipDbPath);
#endif
                if (_qqwryDBFileBytes != null)
                {
                    _qqwryDBFileBytes = null;
                }
                _qqwryDBFileBytes = FileToBytes(ipDbPath);
                using (var stream = new MemoryStream(_qqwryDBFileBytes))
                {
                    _ipIndexCache = BlockToArray(ReadIpBlock(stream, out _startPosition));
                }
            }
            finally
            {
                _initLock.Release();
            }

            if (_qqwryDBFileBytes == null)
            {
                throw new InvalidOperationException("无法打开IP数据库" + _qqwryOptions.DbPath + "！");
            }

            _inited = true;
            return true;
        }

        ///<summary>
        /// 获取指定IP所在地理位置
        ///</summary>
        ///<param name="strIp">要查询的IP地址</param>
        ///<returns></returns>
        public async Task<IpLocation> GetIpLocationAsync(string strIp, CancellationToken token = default(CancellationToken))
        {
            var loc = new IpLocation
            {
                Ip = strIp
            };
            if (!CheckIp(strIp))
            {
                return loc;
            }
            long ip = IpToLong(strIp);
            if (ip == IpToLong("127.0.0.1"))
            {
                loc.Country = "本机内部环回地址";
                loc.Area = string.Empty;
                return loc;
            }
            if (!await InitAsync(token))
            {
                return loc;
            }
            long offset = SearchIp(ip, _ipIndexCache, 0, _ipIndexCache.Length) * 7 + 4;
            using (var stream = new MemoryStream(_qqwryDBFileBytes))
            {
                //重设置起始位置
                stream.Position = _startPosition;
                //跳过起始IP
                stream.Position += offset;
                //跳过结束IP
                stream.Position = ReadLongX(stream, 3) + 4;

                //读取标志
                var flag = stream.ReadByte();
                //表示国家和地区被转向
                if (flag == 1)
                {
                    stream.Position = ReadLongX(stream, 3);
                    //再读标志
                    flag = stream.ReadByte();
                }
                var countryOffset = stream.Position;
                loc.Country = ReadString(stream, flag);

                if (flag == 2)
                {
                    stream.Position = countryOffset + 3;
                }
                flag = stream.ReadByte();
                loc.Area = ReadString(stream, flag);
            }
            if (" CZ88.NET".Equals(loc.Area, StringComparison.CurrentCultureIgnoreCase))
            {
                loc.Area = string.Empty;
            }
            return loc;
        }
        #endregion

        /// <summary>
        /// 检查IP合法性
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool CheckIp(string ip)
        {
            return IpAddressRegex.IsMatch(ip);
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            _initLock?.Dispose();
            _versionLock = null;
            _qqwryDBFileBytes = null;
            _qqwryDBFileBytes = null;
            _ipIndexCache = null;
            _inited = null;
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

        private static byte[] FileToBytes(string fileName)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] bytes = new byte[fileStream.Length];

                fileStream.Read(bytes, 0, bytes.Length);

                fileStream.Close();

                return bytes;
            }
        }

        #endregion
    }
}
