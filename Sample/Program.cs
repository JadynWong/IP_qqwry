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


            var config = new QQWryOptions()
            {
                DbPath = MapRootPath("~/IP/qqwry.dat")
            };

            #region QQWry
            Console.WriteLine("");
            Console.WriteLine("QQWry");
            var ipSearch = new QQWryIpSearch(config);
            ipSearch.Init(true);
            ipSearch.GetIpLocation("52.202.142.95");
            for (var i = 0; i < 100; i++)
            {
                var ipLocation = ipSearch.GetIpLocation(GetRandomIp(ipSearch));
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
                for (var i = 0; i < 100; i++)
                {
                    var ipLocation = ipSearch.GetIpLocation(GetRandomIp(ipSearch));
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
            for (var i = 0; i < 100; i++)
            {
                var ip = GetRandomIp(ipSearch);
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
