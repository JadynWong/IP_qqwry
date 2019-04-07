using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using QQWry;
using QQWry.DependencyInjection;

namespace Sample
{
    class Program
    {
        private static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine("QQWry Sample!");
            var preSearchIpArray = new[]{
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

            var config = new QQWryOptions()
            {
                DbPath = MapRootPath("~/IP/qqwry.dat")
            };

            #region QQWry
            Console.WriteLine("");
            Console.WriteLine("QQWry");
            var ipSearch = new QQWryIpSearch(config);

            foreach (var ip in preSearchIpArray)
            {
                var ipLocation = ipSearch.GetIpLocation(ip);
                Write(ipLocation);
            }
            Console.WriteLine("记录总数" + ipSearch.IpCount);
            Console.WriteLine("版本" + ipSearch.Version);

            #endregion

            #region QQWry.DependencyInjection
            Console.WriteLine("");
            Console.WriteLine("QQWry.DependencyInjection");
            var service = new ServiceCollection();

            service.AddQQWry(config);

            var serviceProvider = service.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var ipSearchInterface = scope.ServiceProvider.GetRequiredService<IIpSearch>();
                foreach (var ip in preSearchIpArray)
                {
                    var ipLocation = ipSearchInterface.GetIpLocation(ip);
                    Write(ipLocation);
                }
                Console.WriteLine("记录总数" + ipSearchInterface.IpCount);
                Console.WriteLine("版本" + ipSearchInterface.Version);
            }


            #endregion

            #region java to QQWry
            Console.WriteLine("");
            Console.WriteLine("java to QQWry");
            var qqWry = new Java2QQWry(config.DbPath);
            foreach (var ip in preSearchIpArray)
            {
                var ipLocation = qqWry.SearchIPLocation(ip);
                Write(ip, ipLocation);
            }

            #endregion

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

}
