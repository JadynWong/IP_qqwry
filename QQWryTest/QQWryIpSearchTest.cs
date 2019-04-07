using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QQWry;
using Xunit;

namespace QQWryTest
{
    public class QQWryIpSearchTest
    {
        protected QQWryIpSearch GetInstance(bool getNewDbFile = false)
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "qqwry.dat");
            if (getNewDbFile)
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }


            var option = new QQWryOptions(dbPath)
            {
                QQWryUrl = "https://github.com/JadynWong/IP_qqwry/raw/test/qqwry.rar",
                CopyWriteUrl = "https://github.com/JadynWong/IP_qqwry/raw/test/copywrite.rar"
            };
            QQWryIpSearch ipSearch = new QQWryIpSearch(option);
            return ipSearch;
        }

        protected string GetRandomIp(IIpSearch ipSearch)
        {
            while (true)
            {
                Random sj = new Random(Guid.NewGuid().GetHashCode());
                var s = "";
                for (int i = 0; i <= 3; i++)
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

        [Fact]
        public void UpdateDbTest()
        {

            var ipSearch = GetInstance(true);

            ipSearch.UpdateDb();

            Assert.True(true);

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public void InitTest()
        {
            var ipSearch = GetInstance();

            var inited = ipSearch.Init();

            Assert.True(inited);

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public void GetIpLocationTest()
        {

            var ipSearch = GetInstance();

            var preSearchIpArray = new string[10];
            for (int i = 0; i < preSearchIpArray.Length; i++)
            {
                preSearchIpArray[i] = GetRandomIp(ipSearch);
            }

            foreach (var ip in preSearchIpArray)
            {
                var ipLocation = ipSearch.GetIpLocation(ip);
                Console.WriteLine($"ip：{ipLocation.Ip}，country：{ipLocation.Country}，area：{ipLocation.Area}");
                Assert.NotNull(ipLocation.Area);
            }

            Console.WriteLine("记录总数" + ipSearch.IpCount);

            Console.WriteLine("版本" + ipSearch.Version);

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public async Task UpdateDbAsyncTestAsync()
        {
            var ipSearch = GetInstance(true);

            await ipSearch.UpdateDbAsync();

            Assert.True(true);

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public async Task InitAsyncTestAsync()
        {
            var ipSearch = GetInstance();

            var inited = await ipSearch.InitAsync();

            Assert.True(inited);

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public async Task GetIpLocationAsyncTestAsync()
        {
            var ipSearch = GetInstance();

            var preSearchIpArray = new string[10];
            for (int i = 0; i < preSearchIpArray.Length; i++)
            {
                preSearchIpArray[i] = GetRandomIp(ipSearch);
            }

            foreach (var ip in preSearchIpArray)
            {
                var ipLocation = await ipSearch.GetIpLocationAsync(ip);
                Console.WriteLine("ip：{0}，country：{1}，area：{2}", ipLocation.Ip, ipLocation.Country, ipLocation.Area);
                Assert.NotNull(ipLocation.Area);
            }

            Console.WriteLine("记录总数" + ipSearch.IpCount);

            Console.WriteLine("版本" + ipSearch.Version);

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public void MuitlThreadingSafeTest()
        {
            var ipSearch = GetInstance();

            var maxTask = 100000;
            //for (int i = 0; i < maxTask; i++)
            //{
            //    var ip = GetRandomIp(ipSearch);
            //    var ipLocation = i % 2 == 0 ? await ipSearch.GetIpLocationAsync(ip) : ipSearch.GetIpLocation(ip);
            //    Console.WriteLine("ip：{0}，country：{1}，area：{2}", ipLocation.Ip, ipLocation.Country, ipLocation.Area);
            //    Assert.NotNull(ipLocation.Area);
            //    Console.WriteLine("记录总数" + ipSearch.IpCount);
            //    Console.WriteLine("版本" + ipSearch.Version);
            //    Assert.True(ipSearch.IpCount > 0);
            //    Assert.NotNull(ipSearch.Version);
            //}

            Parallel.For(0, maxTask, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 1000
            }, async (num, ParallelLoopState) =>
            {
                var ip = GetRandomIp(ipSearch);
                var ipLocation = await ipSearch.GetIpLocationAsync(ip);
                Console.WriteLine("ip：{0}，country：{1}，area：{2}", ipLocation.Ip, ipLocation.Country, ipLocation.Area);
                Assert.NotNull(ipLocation.Area);
                Console.WriteLine("记录总数" + ipSearch.IpCount);
                Console.WriteLine("版本" + ipSearch.Version);
                Assert.True(ipSearch.IpCount > 0);
                Assert.NotNull(ipSearch.Version);

                Debug.WriteLine(num);
            });

            ipSearch.Dispose();
        }
    }
}
