using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sample
{
    ///<summary>
    /// QQWry 的摘要说明。
    ///</summary>
    public class Java2QQWry
    {
        //第一种模式
        #region 第一种模式
        /**/
        ///<summary>
        ///第一种模式
        ///</summary>
        #endregion
        private const byte REDIRECT_MODE_1 = 0x01;
        //第二种模式
        #region 第二种模式
        /**/
        ///<summary>
        ///第二种模式
        ///</summary>
#endregion
        private const byte REDIRECT_MODE_2 = 0x02;
        //每条记录长度
        #region 每条记录长度
        /**/
        ///<summary>
        ///每条记录长度
        ///</summary>
#endregion
        private const int IP_RECORD_LENGTH = 7;
        //数据库文件
        #region 数据库文件
        /**/
        ///<summary>
        ///文件对象
        ///</summary>
        #endregion
        private Stream ipFile;
        private const string unCountry = "未知国家";
        private const string unArea = "未知地区";
        //索引开始位置
        #region 索引开始位置
        /**/
        ///<summary>
        ///索引开始位置
        ///</summary>
        #endregion
        private long ipBegin;
        //索引结束位置
        #region 索引结束位置
        /**/
        ///<summary>
        ///索引结束位置
        ///</summary>
        #endregion
        private long ipEnd;
        //IP地址对象
        #region  IP地址对象
        /**/
        ///<summary>
        /// IP对象
        ///</summary>
        #endregion
        private IPLocation loc;
        //存储文本内容
        #region 存储文本内容
        /**/
        ///<summary>
        ///存储文本内容
        ///</summary>
        #endregion
        private byte[] buf;
        //存储3字节
        #region 存储3字节
        /**/
        ///<summary>
        ///存储3字节
        ///</summary>
        #endregion
        private byte[] b3;
        //存储4字节
        #region 存储4字节
        /**/
        ///<summary>
        ///存储4字节IP地址
        ///</summary>
        #endregion
        private byte[] b4;
        //构造函数
        #region 构造函数
        /**/
        ///<summary>
        ///构造函数
        ///</summary>
        ///<param name="ipfile">IP数据库文件绝对路径</param>
        #endregion
        public Java2QQWry(string ipfile)
        {
            buf = new byte[100];
            b3 = new byte[3];
            b4 = new byte[4];
            try
            {
                using (var fs = new FileStream(ipfile, FileMode.Open))
                {
                    try
                    {
                        ipFile = new MemoryStream();
                        fs.CopyTo(ipFile);
                        ipFile.Position = 0;
                        ipBegin = ReadLongX(ipFile, 4);//readLong4(0);
                        ipFile.Position = 4;
                        ipEnd = ReadLongX(ipFile, 4);//readLong4(4);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            loc = new IPLocation();
        }
        //根据IP地址搜索
        #region 根据IP地址搜索
        /**/
        ///<summary>
        ///搜索IP地址搜索
        ///</summary>
        ///<param name="ip"></param>
        ///<returns></returns>
        #endregion
        public IPLocation SearchIPLocation(string ip)
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
                IP[i] = (byte)(Int32.Parse(ipSp[i]) & 0xFF);
            }
            IPLocation local = null;
            long offset = locateIP(IP);
            if (offset != -1)
            {
                local = getIPLocation(offset);
            }
            if (local == null)
            {
                local = new IPLocation();
                local.area = unArea;
                local.country = unCountry;
            }
            return local;
        }
        //取得具体信息
        #region 取得具体信息
        /**/
        ///<summary>
        ///取得具体信息
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        #endregion
        private IPLocation getIPLocation(long offset)
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
                    loc.country = readString(ReadLongX(ipFile, 3));//readString(readLong3());
                    ipFile.Position = countryOffset + 4;
                }
                else
                    loc.country = readString(countryOffset);
                //读取地区标志
                loc.area = readArea(ipFile.Position);
            }
            else if (one == REDIRECT_MODE_2)
            {
                //第二种模式
                loc.country = readString(ReadLongX(ipFile, 3));//readString(readLong3());
                loc.area = readArea(offset + 8);
            }
            else
            {
                //普通模式
                loc.country = readString(--ipFile.Position);
                loc.area = readString(ipFile.Position);
            }
            return loc;
        }
        //取得地区信息
        #region 取得地区信息
        /**/
        ///<summary>
        ///读取地区名称
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        #endregion
        private string readArea(long offset)
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
                    return readString(areaOffset);
                }
            }
            else
            {
                return readString(offset);
            }
        }
        //读取字符串
        #region 读取字符串
        /**/
        ///<summary>
        ///读取字符串
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        #endregion
        private string readString(long offset)
        {
            ipFile.Position = offset;
            int i = 0;
            for (i = 0, buf[i] = (byte)ipFile.ReadByte(); buf[i] != (byte)(0); buf[++i] = (byte)ipFile.ReadByte()) ;
            if (i > 0)
                return Encoding.GetEncoding("gb2312").GetString(buf, 0, i);
            else
                return "";
        }
        //查找IP地址所在的绝对偏移量
        #region 查找IP地址所在的绝对偏移量
        /**/
        ///<summary>
        ///查找IP地址所在的绝对偏移量
        ///</summary>
        ///<param name="ip"></param>
        ///<returns></returns>
        #endregion
        private long locateIP(byte[] ip)
        {
            long m = 0;
            int r;
            //比较第一个IP项
            readIP(ipBegin, b4);
            r = compareIP(ip, b4);
            if (r == 0)
                return ipBegin;
            else if (r < 0)
                return -1;
            //开始二分搜索
            for (long i = ipBegin, j = ipEnd; i < j;)
            {
                m = this.getMiddleOffset(i, j);
                readIP(m, b4);
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
            readIP(m, b4);
            r = compareIP(ip, b4);
            if (r <= 0)
                return m;
            else
                return -1;
        }
        //读出4字节的IP地址
        #region 读出4字节的IP地址
        /**/
        ///<summary>
        ///从当前位置读取四字节,此四字节是IP地址
        ///</summary>
        ///<param name="offset"></param>
        ///<param name="ip"></param>
        #endregion
        private void readIP(long offset, byte[] ip)
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
        //比较IP地址是否相同
        #region 比较IP地址是否相同
        /**/
        ///<summary>
        ///比较IP地址是否相同
        ///</summary>
        ///<param name="ip"></param>
        ///<param name="beginIP"></param>
        ///<returns>0:相等,1:ip大于beginIP,-1:小于</returns>
        #endregion
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
        //比较两个字节是否相等
        #region 比较两个字节是否相等
        /**/
        ///<summary>
        ///比较两个字节是否相等
        ///</summary>
        ///<param name="bsrc"></param>
        ///<param name="bdst"></param>
        ///<returns></returns>
        #endregion
        private int compareByte(byte bsrc, byte bdst)
        {
            if ((bsrc & 0xFF) > (bdst & 0xFF))
                return 1;
            else if ((bsrc ^ bdst) == 0)
                return 0;
            else
                return -1;
        }
        //根据当前位置读取4字节
        #region 根据当前位置读取4字节
        /**/
        ///<summary>
        ///从当前位置读取4字节,转换为长整型
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        #endregion
        private long readLong4(long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            ret |= ((ipFile.ReadByte() << 24) & 0xFF000000);
            return ret;
        }
        //根据当前位置,读取3字节
        #region 根据当前位置,读取3字节
        /**/
        ///<summary>
        ///根据当前位置,读取3字节
        ///</summary>
        ///<param name="offset"></param>
        ///<returns></returns>
        #endregion
        private long readLong3(long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
        }
        //从当前位置读取3字节
        #region 从当前位置读取3字节
        /**/
        ///<summary>
        ///从当前位置读取3字节
        ///</summary>
        ///<returns></returns>
        #endregion
        private long readLong3()
        {
            long ret = 0;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
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
    } //class QQWry
    public class IPLocation
    {
        public String country;
        public String area;
        public IPLocation()
        {
            country = area = "";
        }
        public IPLocation getCopy()
        {
            IPLocation ret = new IPLocation();
            ret.country = country;
            ret.area = area;
            return ret;
        }
    }
}
