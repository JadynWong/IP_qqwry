using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace QQWry
{
    /// <summary>
    /// QQWryIpSearch 请作为单例使用 数据库缓存在内存
    /// </summary>
    public class QQWryIpSearch : IIpSearch, IDisposable
    {
        private static readonly Encoding _encodingGb2312;

        /// <summary>
        /// IP地址正则验证
        /// </summary>
        private static Regex _ipAddressRegex = new(@"(\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b)", RegexOptions.Compiled);

        private readonly SemaphoreSlim _initLock = new(initialCount: 1, maxCount: 1);

        private readonly object _versionLock = new();

        private readonly long _loopbackIP = IpToLong("127.0.0.1");

        private readonly QQWryOptions _qqwryOptions;

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
#if NET45

#else
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            _encodingGb2312 = Encoding.GetEncoding("gb2312");
        }

        public QQWryIpSearch(QQWryOptions options)
        {
            _qqwryOptions = options;
        }

        /// <inheritdoc />
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public virtual bool Init()
        {
            if (_init != null)
            {
                return _init.Value;
            }
            _initLock.Wait();
            try
            {
                if (_init != null)
                {
                    return _init.Value;
                }

                EnsureFileExist(_qqwryOptions.DbPath);

#if DEBUG
                System.Diagnostics.Debug.WriteLine(format: $"使用IP数据库{_qqwryOptions.DbPath}");
#endif

                _qqwryDbBytes = FileToBytes(_qqwryOptions.DbPath);

                _ipIndexCache = BlockToArray(ReadIpBlock(_qqwryDbBytes, out _startPosition));

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

            var ip = IpToLong(strIp);

            if (ip == _loopbackIP)
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

        /// <inheritdoc />
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<bool> InitAsync(CancellationToken token = default)
        {
            if (_init != null)
            {
                return _init.Value;
            }

            await _initLock.WaitAsync(token);

            try
            {
                if (_init != null)
                {
                    return _init.Value;
                }


                EnsureFileExist(_qqwryOptions.DbPath);

#if DEBUG
                System.Diagnostics.Debug.WriteLine("使用IP数据库{0}", _qqwryOptions.DbPath);
#endif
                _qqwryDbBytes = FileToBytes(_qqwryOptions.DbPath);

                _ipIndexCache = BlockToArray(ReadIpBlock(_qqwryDbBytes, out _startPosition));

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
        ///  获取指定IP所在地理位置
        /// </summary>
        /// <param name="strIp">要查询的IP地址</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<IpLocation> GetIpLocationAsync(string strIp, CancellationToken token = default)
        {
            var loc = new IpLocation
            {
                Ip = strIp
            };

            long ip = IpToLong(strIp);
            if (ip == _loopbackIP)
            {
                loc.Country = "本机内部环回地址";
                loc.Area = string.Empty;
                return loc;
            }
            if (!await InitAsync(token))
            {
                return loc;
            }
            return ReadLocation(loc, ip, _startPosition, _ipIndexCache, _qqwryDbBytes);
        }

        /// <inheritdoc />
        /// <summary>
        /// 检查IP合法性
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool CheckIp(string ip)
        {
            return _ipAddressRegex.IsMatch(ip);
        }

        /// <inheritdoc />
        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            _initLock?.Dispose();
            _qqwryDbBytes = null;
            _qqwryDbBytes = null;
            _ipIndexCache = null;
            _init = null;
        }

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
            //二分法 https://baike.baidu.com/item/%E4%BA%8C%E5%88%86%E6%B3%95%E6%9F%A5%E6%89%BE
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
        private static byte[] ReadIpBlock(byte[] bytes, out long startPosition)
        {
            long offset = 0;
            startPosition = ReadLongX(bytes, offset, 4);
            offset += 4;
            var endPosition = ReadLongX(bytes, offset, 4);
            offset = startPosition;
            var count = (endPosition - startPosition) / 7 + 1;//总记录数

            var ipBlock = new byte[count * 7];
            for (var i = 0; i < ipBlock.Length; i++)
            {
                ipBlock[i] = bytes[offset + i];
            }
            return ipBlock;
        }

        /// <summary>
        ///  从IP文件中读取指定字节并转换位long
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="bytesCount">需要转换的字节数，主意不要超过8字节</param>
        /// <returns></returns>
        private static long ReadLongX(byte[] bytes, long offset, int bytesCount)
        {
            var cBytes = new byte[8];
            for (var i = 0; i < bytesCount; i++)
            {
                cBytes[i] = bytes[offset + i];
            }
            return BitConverter.ToInt64(cBytes, 0);
        }

        /// <summary>
        ///  从IP文件中读取字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="flag">转向标志</param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static string ReadString(byte[] bytes, int flag, ref long offset)
        {
            if (flag == 1 || flag == 2)//转向标志
            {
                offset = ReadLongX(bytes, offset, 3);
            }
            else
            {
                offset -= 1;
            }
            var list = new List<byte>();
            var b = (byte)bytes[offset];
            offset += 1;
            while (b > 0)
            {
                list.Add(b);
                b = (byte)bytes[offset];
                offset += 1;
            }
            return _encodingGb2312.GetString(list.ToArray());
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

        private static void EnsureFileExist(string ipDbPath)
        {
            var dir = Path.GetDirectoryName(ipDbPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
            }
            if (!File.Exists(ipDbPath))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(format: "无法找到IP数据库{0}", ipDbPath);
#endif
                throw new Exception($"无法找到IP数据库{ipDbPath}");
            }

        }

        private static IpLocation ReadLocation(IpLocation loc, long ip, long startPosition, long[] ipIndex, byte[] qqwryDbBytes)
        {
            long offset = SearchIp(ip, ipIndex, 0, ipIndex.Length) * 7 + 4;

            //偏移
            var arrayOffset = startPosition + offset;
            //跳过结束IP
            arrayOffset = ReadLongX(qqwryDbBytes, arrayOffset, 3) + 4;
            //读取标志
            var flag = qqwryDbBytes[arrayOffset];
            arrayOffset += 1;
            //表示国家和地区被转向
            if (flag == 1)
            {
                arrayOffset = ReadLongX(qqwryDbBytes, arrayOffset, 3);
                //再读标志
                flag = qqwryDbBytes[arrayOffset];
                arrayOffset += 1;
            }
            var countryOffset = arrayOffset;
            loc.Country = ReadString(qqwryDbBytes, flag, ref arrayOffset);

            if (flag == 2)
            {
                arrayOffset = countryOffset + 3;
            }

            flag = qqwryDbBytes[arrayOffset];
            arrayOffset += 1;
            loc.Area = ReadString(qqwryDbBytes, flag, ref arrayOffset);

            if (" CZ88.NET".Equals(loc.Area, StringComparison.CurrentCultureIgnoreCase))
            {
                loc.Area = string.Empty;
            }

            return loc;
        }
    }
}
