using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using QQWry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
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
        private byte[] _qqwryDbBytes;
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
        private bool? _init;
        /// <summary>
        /// IP地址正则验证
        /// </summary>
        private static Regex IpAddressRegex => new Regex(@"(\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b)");

        private static readonly HttpClient _httpClient;

        private readonly QQWryOptions _qqwryOptions;
        private int? _ipCount;
        private string _version;

        /// <inheritdoc />
        /// <summary>
        /// 记录总数
        /// </summary>
        public int IpCount
        {
            get
            {
                if (!_ipCount.HasValue)
                {
                    Init();
                    _ipCount = _ipIndexCache.Length;
                }

                return _ipCount.Value;
            }
        }

        /// <inheritdoc />
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



        /// <inheritdoc />
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public virtual bool Init(bool getNewDb = false)
        {
            if (_init != null && !getNewDb)
            {
                return _init.Value;
            }
            _initLock.Wait();
            try
            {
                if (_init != null && !getNewDb)
                {
                    return _init.Value;
                }

                var isExist = DbFileExist(_qqwryOptions.DbPath);
                if (!isExist || getNewDb)
                {
                    UpdateDb();
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"使用IP数据库{_qqwryOptions.DbPath}");
#endif

                _qqwryDbBytes = FileToBytes(_qqwryOptions.DbPath);

                using (var stream = new MemoryStream(_qqwryDbBytes))
                {
                    _ipIndexCache = BlockToArray(ReadIpBlock(stream, out _startPosition));
                }
                _ipCount = null;
                _version = null;
                _init = true;
            }
            finally
            {
                _initLock.Release();
            }

            if (_qqwryDbBytes == null)
            {
                throw new InvalidOperationException("无法打开IP数据库" + _qqwryOptions.DbPath + "！");
            }

            return true;

        }

        /// <inheritdoc />
        /// <summary>
        /// 获取CopyWrite
        /// </summary>
        /// <returns></returns>
        public virtual QQWryCopyWrite GetCopyWrite()
        {
            var copywriteStream = _httpClient.GetStreamAsync(_qqwryOptions.CopyWriteUrl).Result;

            if (copywriteStream == null)
            {
                throw new Exception("-1 copywrite can't null");
            }

            return ReadFromStream(copywriteStream);
        }

        /// <inheritdoc />
        /// <summary>
        ///  获取指定IP所在地理位置
        /// </summary>
        /// <param name="strIp">要查询的IP地址</param>
        /// <returns></returns>
        public virtual IpLocation GetIpLocation(string strIp)
        {
            var loc = new IpLocation
            {
                Ip = strIp
            };

            if (!CheckIp(strIp))
            {
                return loc;
            }

            var ip = IpToLong(strIp);

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

            return ReadLocation(loc, ip, _startPosition, _ipIndexCache, _qqwryDbBytes);
        }

        #endregion

        #region async


        /// <inheritdoc />
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<bool> InitAsync(bool getNewDb = false, CancellationToken token = default(CancellationToken))
        {
            if (_init != null && !getNewDb)
            {
                return _init.Value;
            }

            await _initLock.WaitAsync(token);

            try
            {
                if (_init != null && !getNewDb)
                {
                    return _init.Value;
                }


                var isExist = DbFileExist(_qqwryOptions.DbPath);
                if (!isExist || getNewDb)
                {
                    await UpdateDbAsync();
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine("使用IP数据库{0}", _qqwryOptions.DbPath);
#endif
                _qqwryDbBytes = FileToBytes(_qqwryOptions.DbPath);

                using (var stream = new MemoryStream(_qqwryDbBytes))
                {
                    _ipIndexCache = BlockToArray(ReadIpBlock(stream, out _startPosition));
                }
                _ipCount = null;
                _version = null;
                _init = true;
            }
            finally
            {
                _initLock.Release();
            }

            if (_qqwryDbBytes == null)
            {
                throw new InvalidOperationException("无法打开IP数据库" + _qqwryOptions.DbPath + "！");
            }

            return true;
        }

        /// <inheritdoc />
        /// <summary>
        /// 获取CopyWrite
        /// </summary>
        /// <returns></returns>
        public virtual async Task<QQWryCopyWrite> GetCopyWriteAsync()
        {
            var copywriteStream = await _httpClient.GetStreamAsync(_qqwryOptions.CopyWriteUrl).ConfigureAwait(false);
            if (copywriteStream == null)
            {
                throw new Exception("-1 copywrite can't null");
            }

            return ReadFromStream(copywriteStream);
        }

        /// <inheritdoc />
        /// <summary>
        ///  获取指定IP所在地理位置
        /// </summary>
        /// <param name="strIp">要查询的IP地址</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<IpLocation> GetIpLocationAsync(string strIp, CancellationToken token = default(CancellationToken))
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
            if (!await InitAsync(false, token))
            {
                return loc;
            }
            return ReadLocation(loc, ip, _startPosition, _ipIndexCache, _qqwryDbBytes);
        }
        #endregion

        /// <inheritdoc />
        /// <summary>
        /// 检查IP合法性
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool CheckIp(string ip)
        {
            return IpAddressRegex.IsMatch(ip);
        }

        /// <inheritdoc />
        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            _initLock?.Dispose();
            _versionLock = null;
            _qqwryDbBytes = null;
            _qqwryDbBytes = null;
            _ipIndexCache = null;
            _init = null;
        }

        #endregion

        #region private

        /// <summary>
        /// 更新数据库
        /// </summary>
        private void UpdateDb()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("更新IP数据库{0}", _qqwryOptions.DbPath);
#endif
            var copyWrite = GetCopyWrite();
            var qqwry = _httpClient.GetByteArrayAsync(_qqwryOptions.QQWryUrl).Result;

            ExtractWriteDbFile(copyWrite, qqwry, _qqwryOptions.DbPath);
        }

        /// <summary>
        /// 更新数据库
        /// </summary>
        private async Task UpdateDbAsync()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("更新IP数据库{0}", _qqwryOptions.DbPath);
#endif
            var copyWrite = await GetCopyWriteAsync();

            var qqwry = await _httpClient.GetByteArrayAsync(_qqwryOptions.QQWryUrl).ConfigureAwait(false);

            ExtractWriteDbFile(copyWrite, qqwry, _qqwryOptions.DbPath);

            _ipCount = null;

            _version = null;
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

        private static bool DbFileExist(string ipDbPath)
        {
            var dir = Path.GetDirectoryName(ipDbPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
            }
            if (!File.Exists(ipDbPath))
            {
                Console.WriteLine("无法找到IP数据库{0}", ipDbPath);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 解压并写入文件
        /// </summary>
        /// <param name="copyWrite"></param>
        /// <param name="qqwry"></param>
        /// <param name="path"></param>
        private static void ExtractWriteDbFile(QQWryCopyWrite copyWrite, byte[] qqwry, string path)
        {
            //extract information from copywrite.rar
            if (qqwry.Length <= 24 || copyWrite.Sign != "CZIP")
            {
                throw new Exception("-2 sign error");
            }

            if (qqwry.Length != copyWrite.Size)
            {
                throw new Exception("-4 size error");
            }
            //decrypt
            var head = new byte[0x200];
            var key = copyWrite.Key;
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
                using (var fsOut = File.Create(path))
                {
                    //inflaterInputStream.CopyTo(fsOut);
                    StreamUtils.Copy(inflaterInputStream, fsOut, dataBuffer);
                }
            }
        }

        private static IpLocation ReadLocation(IpLocation loc, long ip, long startPosition, long[] ipIndex, byte[] qqwryDbBytes)
        {
            long offset = SearchIp(ip, ipIndex, 0, ipIndex.Length) * 7 + 4;

            using (var stream = new MemoryStream(qqwryDbBytes))
            {
                stream.Position = startPosition;
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

        private static QQWryCopyWrite ReadFromStream(Stream copywriteStream)
        {
            var binaryReader = new BinaryReader(copywriteStream);
            var copyWrite = new QQWryCopyWrite()
            {
                Sign = Encoding.GetEncoding("gb2312").GetString(binaryReader.ReadBytesLE(4).Where(x => x != 0x00).ToArray()),
                Version = binaryReader.ReadUInt32LE(),
                Unknown1 = binaryReader.ReadUInt32LE(),
                Size = binaryReader.ReadUInt32LE(),
                Unknown2 = binaryReader.ReadUInt32LE(),
                Key = binaryReader.ReadUInt32LE(),
                Text = Encoding.GetEncoding("gb2312").GetString(binaryReader.ReadBytesLE(128).Where(x => x != 0x00).ToArray()),
                Link = Encoding.GetEncoding("gb2312").GetString(binaryReader.ReadBytesLE(128).Where(x => x != 0x00).ToArray())
            };
            return copyWrite;
        }

        #endregion
    }
}
