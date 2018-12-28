using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine("Hello World!");
            IpConfig config = new IpConfig()
            {
                IpDbPath = "~/IP/qqwry.dat"
            };
            var ipSearch = new MyIpSearch(config);
            ipSearch.Init();
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
                "255.255.255.0"
            };
            foreach (var ip in ips)
            {
                var ipLocation = ipSearch.GetIpLocation(ip);
                Write(ipLocation);
            }

            //QQWry qWry = new QQWry(MapRootPath(config.IpDbPath));
            //foreach (var ip in ips)
            //{
            //    var ipLocation = qWry.SearchIPLocation(ip);
            //    Write(ip, ipLocation);
            //}

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
}
