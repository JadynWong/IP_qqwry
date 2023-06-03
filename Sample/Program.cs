using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

using Microsoft.Extensions.DependencyInjection;

using QQWry;
using QQWry.DependencyInjection;

namespace Sample
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("QQWry Sample!");

            var config = new QQWryOptions()
            {
                DbPath = BenchmarkTest.MapRootPath("qqwry.dat")
            };
            #region QQWry
            Console.WriteLine("");
            Console.WriteLine("QQWry");

            var ipSearch = new QQWryIpSearch(config);
            var ipSearchMode2 = new QQWryIpSearchMode2(config);

            //可选, 若不调用则在首次使用时, 自动先初始化
            await ipSearch.InitAsync();
            await ipSearchMode2.InitAsync();

            for (var i = 0; i < 10; i++)
            {
                var ipLocation = await ipSearch.GetIpLocationAsync(BenchmarkTest.GetRandomIp(ipSearch));
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
                for (var i = 0; i < 10; i++)
                {
                    var ipLocation = await ipSearch.GetIpLocationAsync(BenchmarkTest.GetRandomIp(ipSearch));
                    Write(ipLocation);
                }
                Console.WriteLine("记录总数" + ipSearchInterface.IpCount);
                Console.WriteLine("版本" + ipSearchInterface.Version);
            }


            #endregion

            #region Speed

            Console.WriteLine("");
            Console.WriteLine("Speed");

            await ExecuteBatchAsync(100, ipSearch);
            await ExecuteBatchAsync(1_000, ipSearch);
            await ExecuteBatchAsync(10_000, ipSearch);
            await ExecuteBatchAsync(100_000, ipSearch);
            await ExecuteBatchAsync(1_000_000, ipSearch);

            #endregion

#if RELEASE
            var summary = BenchmarkRunner.Run<BenchmarkTest>();
#endif

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

        private static async Task ExecuteBatchAsync(int count, IIpSearch ipSearch)
        {
            // prepare
            var ips = Enumerable.Range(0, count).Select(_ => BenchmarkTest.GetRandomIp(ipSearch)).ToArray();

            var sw = Stopwatch.StartNew();

            foreach (var ip in ips)
            {
                var _ = await ipSearch.GetIpLocationAsync(ip);
            }

            Console.WriteLine("{0}条耗费{1}ms", count, sw.ElapsedMilliseconds);
        }
    }
}
