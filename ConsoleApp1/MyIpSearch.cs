using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    public class MyIpSearch : IDisposable
    {
        private readonly object _lockInit = new object();
        private readonly object _lockRead = new object();
        private MemoryStream IpFile;
        private long[] IpArray;
        private long StartPosition;
        private bool? Inited;

        private static Regex IpAddressRegex => new Regex(@"(\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b)");

        private readonly IpConfig _ipConfig;
        private int? ipCount;
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
                    ipCount = IpArray.Length;
                }

                return ipCount.Value;
            }
        }
        private string _version;
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

        public MyIpSearch(IpConfig ipConfig)
        {
            _ipConfig = ipConfig;
        }

       

        /// <summary>
        /// Maps a virtual path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "~/bin"</param>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
        public virtual string MapRootPath(string path)
        {
            path = path.Replace("~/", "").TrimStart('/').Replace('/', '\\');
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, path);
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

                var ipDbPath = MapRootPath(_ipConfig.IpDbPath);

                if (!File.Exists(ipDbPath))
                {
                    Console.WriteLine("无法找到IP数据库{0}", ipDbPath);
                    return false;
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("使用IP数据库{0}", ipDbPath);
#endif
                using (var fs = File.OpenRead(ipDbPath))
                {
                    try
                    {
                        IpFile = new MemoryStream();
                        fs.CopyTo(IpFile);
                        IpFile.Position = 0;
                        IpArray = BlockToArray(ReadIpBlock(IpFile, out var StartPosition));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);

                        return false;
                    }
                }
            }

            if (IpFile == null)
            {
                throw new InvalidOperationException("无法打开IP数据库" + _ipConfig.IpDbPath + "！");
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
            if (!CheckIp(strIp))
            {
                return loc;
            }
            long ip = IpToLong(strIp);
            if ((ip >= IpToLong("127.0.0.1") && (ip <= IpToLong("127.255.255.255"))))
            {
                loc.Country = "本机内部环回地址";
                loc.Area = string.Empty;
            }
            else
            {
                if ((((ip >= IpToLong("0.0.0.0")) && (ip <= IpToLong("2.255.255.255"))) || ((ip >= IpToLong("64.0.0.0")) && (ip <= IpToLong("126.255.255.255")))) ||
                ((ip >= IpToLong("58.0.0.0")) && (ip <= IpToLong("60.255.255.255"))))
                {
                    loc.Country = "网络保留地址";
                    loc.Area = string.Empty;
                }
            }
            if (!Init())
            {
                return loc;
            }
            long offset = SearchIp(ip, IpArray, 0, IpArray.Length) * 7 + 4;
            lock (_lockRead)
            {
                IpFile.Position = StartPosition;
                //跳过起始IP
                IpFile.Position += offset;
                //跳过结束IP
                IpFile.Position = ReadLongX(IpFile, 3) + 4;

                //读取标志
                var flag = IpFile.ReadByte();
                //表示国家和地区被转向
                if (flag == 1)
                {
                    IpFile.Position = ReadLongX(IpFile, 3);
                    //再读标志
                    flag = IpFile.ReadByte();
                }
                var countryOffset = IpFile.Position;
                loc.Country = ReadString(IpFile, flag);

                if (flag == 2)
                {
                    IpFile.Position = countryOffset + 3;
                }
                flag = IpFile.ReadByte();
                loc.Area = ReadString(IpFile, flag);
                if (" CZ88.NET".Equals(loc.Area, StringComparison.CurrentCultureIgnoreCase))
                {
                    loc.Area = string.Empty;
                }
                return loc;
            }

        }

        ///<summary>
        /// 将字符串形式的IP转换位long
        ///</summary>
        ///<param name="strIp"></param>
        ///<returns></returns>
        private long IpToLong(string strIp)
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

        public void Dispose()
        {
            IpFile?.Dispose();
            IpFile = null;
            IpArray = null;
            Inited = null;
        }
    }
}
