using System;
using System.IO;
using System.Linq;
using System.Text;

using BenchmarkDotNet.Running;

using Microsoft.Extensions.DependencyInjection;

using QQWry;
using QQWry.DependencyInjection;

namespace Sample
{
    class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("QQWry Sample!");

            var config = new QQWryOptions()
            {
                DbPath = Test.MapRootPath("qqwry.dat")
            };
            #region QQWry
            Console.WriteLine("");
            Console.WriteLine("QQWry");

            var ipSearch = new QQWryIpSearch(config);
            var ipSearchMode2 = new QQWryIpSearchMode2(config);

            var copyWrite = ipSearch.GetCopyWrite();
            var date = copyWrite.Text.Replace("纯真IP地址数据库 ", string.Empty);
            var getNewDb = ipSearch.Version.IndexOf(date) == -1;

            ipSearch.Init(getNewDb);
            ipSearchMode2.Init(getNewDb);

            for (var i = 0; i < 100; i++)
            {
                var ipLocation = ipSearchMode2.GetIpLocation(Test.GetRandomIp(ipSearchMode2));
                Write(ipLocation);
            }
            Console.WriteLine("记录总数" + ipSearchMode2.IpCount);
            Console.WriteLine("版本" + ipSearchMode2.Version);

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
                for (var i = 0; i < 100; i++)
                {
                    var ipLocation = ipSearch.GetIpLocation(Test.GetRandomIp(ipSearch));
                    Write(ipLocation);
                }
                Console.WriteLine("记录总数" + ipSearchInterface.IpCount);
                Console.WriteLine("版本" + ipSearchInterface.Version);
            }


            #endregion

            #region java to QQWry
            Console.WriteLine("");
            Console.WriteLine("java to QQWry");
            var javaQQWry = new Java2QQWry(config.DbPath);
            for (var i = 0; i < 100; i++)
            {
                var ip = Test.GetRandomIp(ipSearch);
                var ipLocation = javaQQWry.SearchIPLocation(ip);
                Write(ip, ipLocation);
            }

            #endregion

            var summary = BenchmarkRunner.Run<Test>();

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
        /// <returns>The physical path. E.g. "c:\\inetpub\\wwwroot\\bin"</returns>
        public static string MapRootPath(string path)
        {
            path = path.Replace("~/", "").TrimStart('/').Replace('/', '\\');
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, path);
        }

        static string GetRandomIp(IIpSearch ipSearch)
        {
            while (true)
            {
                var sj = new Random(Guid.NewGuid().GetHashCode());
                var s = "";
                for (var i = 0; i <= 3; i++)
                {
                    var q = sj.Next(0, 255).ToString();
                    if (i < 3)
                    {
                        s += (q + ".").ToString();
                    }
                    else
                    {
                        s += q.ToString();
                    }
                }
                if (ipSearch.CheckIp(s))
                {
                    return s;
                }
            }
        }
    }

}
