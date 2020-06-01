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
    public class QQWryIpSearchMode2 : IDisposable, IIpSearch
    {
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private object _versionLock = new object();


        ///<summary>
        ///第一种模式
        ///</summary>
        private const byte REDIRECT_MODE_1 = 0x01;

        ///<summary>
        ///第二种模式
        ///</summary>
        private const byte REDIRECT_MODE_2 = 0x02;

        ///<summary>
        ///每条记录长度
        ///</summary>
        private const int IP_RECORD_LENGTH = 7;

        private const string unCountry = "未知国家";
        private const string unArea = "未知地区";

        private readonly Encoding _encodingGb2312;

        /// <summary>
        /// 数据库 缓存
        /// </summary>
        private byte[] _qqwryDbBytes;

        /// <summary>
        /// 起始定位
        /// </summary>
        private long _startPosition;

        /// <summary>
        /// 结束定位
        /// </summary>
        private long _endPosition;

        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool? _init;

        /// <summary>
        /// IP地址正则验证
        /// </summary>
        private static Regex IpAddressRegex => new Regex(@"(\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b)");

        private readonly HttpClient _httpClient;

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

        static QQWryIpSearchMode2()
        {
#if NET45

#else
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        public QQWryIpSearchMode2(QQWryOptions options)
        {
            _qqwryOptions = options;
            _httpClient = new HttpClient();
            _encodingGb2312 = Encoding.GetEncoding("gb2312");
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
                System.Diagnostics.Debug.WriteLine(format: $"使用IP数据库{_qqwryOptions.DbPath}");
#endif

                _qqwryDbBytes = FileToBytes(_qqwryOptions.DbPath);

                var ipFile = new IpDbAccessor(_qqwryDbBytes);
                _startPosition = ReadLongX(ipFile, 4);
                _endPosition = ReadLongX(ipFile, 4);

                //总记录数
                _ipCount = Convert.ToInt32((_endPosition - _startPosition) / IP_RECORD_LENGTH + 1);

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
            var request = new HttpRequestMessage(HttpMethod.Get, _qqwryOptions.CopyWriteUrl)
            {
                Version = new Version(1, 1)
            };
            if (_qqwryOptions.CopyWriteUrl.IndexOf("cz88.net", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                request.Headers.Add("Accept", "text/html, */*");
                request.Headers.Add("User-Agent", "Mozilla/3.0 (compatible; Indy Library)");
            }

            var response = _httpClient.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            var copywriteStream = response.Content.ReadAsStreamAsync().Result;

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

            return ReadLocation(loc, strIp);
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

                var ipFile = new IpDbAccessor(_qqwryDbBytes);
                _startPosition = ReadLongX(ipFile, 4);
                _endPosition = ReadLongX(ipFile, 4);

                //总记录数
                _ipCount = Convert.ToInt32((_endPosition - _startPosition) / IP_RECORD_LENGTH + 1);

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
            var request = new HttpRequestMessage(HttpMethod.Get, _qqwryOptions.CopyWriteUrl)
            {
                Version = new Version(1, 1)
            };
            if (_qqwryOptions.CopyWriteUrl.IndexOf("cz88.net", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                request.Headers.Add("Accept", "text/html, */*");
                request.Headers.Add("User-Agent", "Mozilla/3.0 (compatible; Indy Library)");
            }
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var copywriteStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

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
            return ReadLocation(loc, strIp);
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
            System.Diagnostics.Debug.WriteLine(format: "更新IP数据库{0}", _qqwryOptions.DbPath);
#endif
            var copyWrite = GetCopyWrite();
            var request = new HttpRequestMessage(HttpMethod.Get, _qqwryOptions.QQWryUrl)
            {
                Version = new Version(1, 1)
            };
            if (_qqwryOptions.CopyWriteUrl.IndexOf("cz88.net", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                request.Headers.Add("Accept", "text/html, */*");
                request.Headers.Add("User-Agent", "Mozilla/3.0 (compatible; Indy Library)");
            }
            var response = _httpClient.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            var qqwry = response.Content.ReadAsByteArrayAsync().Result;

            ExtractWriteDbFile(copyWrite, qqwry, _qqwryOptions.DbPath);
        }

        /// <summary>
        /// 更新数据库
        /// </summary>
        private async Task UpdateDbAsync()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(format: "更新IP数据库{0}", _qqwryOptions.DbPath);
#endif
            var copyWrite = await GetCopyWriteAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, _qqwryOptions.QQWryUrl)
            {
                Version = new Version(1, 1)
            };
            if (_qqwryOptions.CopyWriteUrl.IndexOf("cz88.net", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                request.Headers.Add("Accept", "text/html, */*");
                request.Headers.Add("User-Agent", "Mozilla/3.0 (compatible; Indy Library)");
            }
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var qqwry = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            ExtractWriteDbFile(copyWrite, qqwry, _qqwryOptions.DbPath);

            _ipCount = null;

            _version = null;
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
#if DEBUG
                System.Diagnostics.Debug.WriteLine(format: "无法找到IP数据库{0}", ipDbPath);
#endif
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

        ///<summary>
        ///搜索IP地址搜索
        ///</summary>
        ///<param name="ip"></param>
        ///<returns></returns>
        public IpLocation ReadLocation(IpLocation loc, string ip)
        {
            //将字符IP转换为字节
            string[] ipSp = ip.Split('.');
            if (ipSp.Length != 4)
            {
                throw new ArgumentOutOfRangeException("不是合法的IP地址!");
            }
            byte[] IP = new byte[4];
            for (int i = 0; i < IP.Length; i++)
            {
                IP[i] = (byte)(int.Parse(ipSp[i]) & 0xFF);
            }
            var ipFile = new IpDbAccessor(_qqwryDbBytes);
            long offset = locateIP(ipFile, IP);
            if (offset != -1)
            {
                loc = getIPLocation(loc, ipFile, offset);
            }
            return loc;
        }

        ///<summary>
        ///取得具体信息
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        private IpLocation getIPLocation(IpLocation loc, IpDbAccessor ipFile, long offset)
        {
            ipFile.Position = offset + 4;
            //读取第一个字节判断是否是标志字节
            byte one = (byte)ipFile.ReadByte();
            if (one == REDIRECT_MODE_1)
            {
                //第一种模式
                //读取国家偏移
                long countryOffset = ReadLongX(ipFile, 3);//readLong3();
                //转至偏移处
                ipFile.Position = countryOffset;
                //再次检查标志字节
                byte b = (byte)ipFile.ReadByte();
                if (b == REDIRECT_MODE_2)
                {
                    loc.Country = readString(ipFile, ReadLongX(ipFile, 3));//readString(readLong3());
                    ipFile.Position = countryOffset + 4;
                }
                else
                    loc.Country = readString(ipFile, countryOffset);
                //读取地区标志
                loc.Area = readArea(ipFile, ipFile.Position);
            }
            else if (one == REDIRECT_MODE_2)
            {
                //第二种模式
                loc.Country = readString(ipFile, ReadLongX(ipFile, 3));//readString(readLong3());
                loc.Area = readArea(ipFile, offset + 8);
            }
            else
            {
                //普通模式
                loc.Country = readString(ipFile, --ipFile.Position);
                loc.Area = readString(ipFile, ipFile.Position);
            }
            return loc;
        }

        ///<summary>
        ///读取地区名称
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        private string readArea(IpDbAccessor ipFile, long offset)
        {
            ipFile.Position = offset;
            byte one = (byte)ipFile.ReadByte();
            if (one == REDIRECT_MODE_1 || one == REDIRECT_MODE_2)
            {
                ipFile.Position = offset + 1;
                long areaOffset = ReadLongX(ipFile, 3);//readLong3(offset + 1);
                if (areaOffset == 0)
                    return unArea;
                else
                {
                    return readString(ipFile, areaOffset).Replace(" CZ88.NET", "");
                }
            }
            else
            {
                return readString(ipFile, offset).Replace(" CZ88.NET","");
            }
        }

        ///<summary>
        ///读取字符串
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        private string readString(IpDbAccessor ipFile, long offset)
        {
            var buf = new byte[100];
            ipFile.Position = offset;
            int i = 0;
            for (i = 0, buf[i] = (byte)ipFile.ReadByte();
                buf[i] != (byte)(0); buf[++i] = (byte)ipFile.ReadByte()) ;
            if (i > 0)
                return _encodingGb2312.GetString(buf, 0, i);
            else
                return "";
        }

        ///<summary>
        ///查找IP地址所在的绝对偏移量
        ///</summary>
        ///<param name="ip"></param>
        ///<returns></returns>
        private long locateIP(IpDbAccessor ipFile, byte[] ip)
        {
            long m = 0;
            int r;
            var b4 = new byte[4];
            //比较第一个IP项
            readIP(ipFile, _startPosition, b4);
            r = compareIP(ip, b4);
            if (r == 0)
                return _startPosition;
            else if (r < 0)
                return -1;
            //开始二分搜索
            for (long i = _startPosition, j = _endPosition; i < j;)
            {
                m = this.getMiddleOffset(i, j);
                readIP(ipFile, m, b4);
                r = compareIP(ip, b4);
                if (r > 0)
                    i = m;
                else if (r < 0)
                {
                    if (m == j)
                    {
                        j -= IP_RECORD_LENGTH;
                        m = j;
                    }
                    else
                    {
                        j = m;
                    }
                }
                else
                {
                    ipFile.Position = m + 4;
                    return ReadLongX(ipFile, 3);//readLong3(m + 4);
                }

            }
            ipFile.Position = m + 4;
            m = ReadLongX(ipFile, 3);//readLong3(m + 4);
            readIP(ipFile, m, b4);
            r = compareIP(ip, b4);
            if (r <= 0)
                return m;
            else
                return -1;
        }

        ///<summary>
        ///从当前位置读取四字节,此四字节是IP地址
        ///</summary>
        ///<param name="offset"></param>
        ///<param name="ip"></param>
        private void readIP(IpDbAccessor ipFile, long offset, byte[] ip)
        {
            ipFile.Position = offset;
            ipFile.Read(ip, 0, ip.Length);
            byte tmp = ip[0];
            ip[0] = ip[3];
            ip[3] = tmp;
            tmp = ip[1];
            ip[1] = ip[2];
            ip[2] = tmp;
        }

        ///<summary>
        ///比较IP地址是否相同
        ///</summary>
        ///<param name="ip"></param>
        ///<param name="beginIP"></param>
        ///<returns>0:相等,1:ip大于beginIP,-1:小于</returns>
        private int compareIP(byte[] ip, byte[] beginIP)
        {
            for (int i = 0; i < 4; i++)
            {
                int r = compareByte(ip[i], beginIP[i]);
                if (r != 0)
                    return r;
            }
            return 0;
        }

        ///<summary>
        ///比较两个字节是否相等
        ///</summary>
        ///<param name="bsrc"></param>
        ///<param name="bdst"></param>
        ///<returns></returns>
        private int compareByte(byte bsrc, byte bdst)
        {
            if ((bsrc & 0xFF) > (bdst & 0xFF))
                return 1;
            else if ((bsrc ^ bdst) == 0)
                return 0;
            else
                return -1;
        }

        ///<summary>
        ///从当前位置读取4字节,转换为长整型
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        private long readLong4(IpDbAccessor ipFile, long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= Convert.ToInt64(ipFile.ReadByte() & 0xFF);
            ret |= Convert.ToInt64((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= Convert.ToInt64((ipFile.ReadByte() << 16) & 0xFF0000);
            ret |= Convert.ToInt64((ipFile.ReadByte() << 24) & 0xFF000000);
            return ret;
        }

        ///<summary>
        ///根据当前位置,读取3字节
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        private long readLong3(IpDbAccessor ipFile, long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= Convert.ToInt64(ipFile.ReadByte() & 0xFF);
            ret |= Convert.ToInt64((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= Convert.ToInt64((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
        }

        ///<summary>
        ///从当前位置读取3字节
        ///</summary>
        ///<returns></returns>
        private long readLong3(IpDbAccessor ipFile)
        {
            long ret = 0;
            ret |= Convert.ToInt64(ipFile.ReadByte() & 0xFF);
            ret |= Convert.ToInt64((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= Convert.ToInt64((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
        }

        /// <summary>
        ///  从IP文件中读取指定字节并转换位long
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytesCount">需要转换的字节数，主意不要超过8字节</param>
        /// <returns></returns>
        private long ReadLongX(IpDbAccessor stream, int bytesCount)
        {
            var bytes = new byte[8];
            stream.Read(bytes, 0, bytesCount);
            return BitConverter.ToInt64(bytes, 0);
        }

        //取得begin和end之间的偏移量
        #region 取得begin和end之间的偏移量
        /**/
        ///<summary>
        ///取得begin和end中间的偏移
        ///</summary>
        ///<param name="begin"></param>
        ///<param name="end"></param>
        ///<returns></returns>
        #endregion
        private long getMiddleOffset(long begin, long end)
        {
            long records = (end - begin) / IP_RECORD_LENGTH;
            records >>= 1;
            if (records == 0)
                records = 1;
            return begin + records * IP_RECORD_LENGTH;
        }

        private QQWryCopyWrite ReadFromStream(Stream copywriteStream)
        {
            var binaryReader = new BinaryReader(copywriteStream);
            var copyWrite = new QQWryCopyWrite()
            {
                Sign = _encodingGb2312.GetString(binaryReader.ReadBytesLE(4).Where(x => x != 0x00).ToArray()),
                Version = binaryReader.ReadUInt32LE(),
                Unknown1 = binaryReader.ReadUInt32LE(),
                Size = binaryReader.ReadUInt32LE(),
                Unknown2 = binaryReader.ReadUInt32LE(),
                Key = binaryReader.ReadUInt32LE(),
                Text = _encodingGb2312.GetString(binaryReader.ReadBytesLE(128).Where(x => x != 0x00).ToArray()),
                Link = _encodingGb2312.GetString(binaryReader.ReadBytesLE(128).Where(x => x != 0x00).ToArray())
            };
            return copyWrite;
        }

        #endregion
    }
}
