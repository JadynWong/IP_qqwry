using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

// ReSharper disable InconsistentNaming
namespace QQWry.Test
{
    public class QQWryIpSearchTest
    {
        protected QQWryIpSearch GetInstance()
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "qqwry.dat");
            var option = new QQWryOptions(dbPath);
            return new QQWryIpSearch(option);
        }

        protected string GetRandomIp(IIpSearch ipSearch)
        {
            while (true)
            {
                var s = "";
                for (var i = 0; i <= 3; i++)
                {
                    var q = Random.Shared.Next(0, 255).ToString();
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
        public void CheckTest()
        {
            var ipSearch = GetInstance();

            var ip = GetRandomIp(ipSearch);

            Assert.NotEmpty(ip);

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
        public async Task InitAsyncTest()
        {
            var ipSearch = GetInstance();

            var inited = await ipSearch.InitAsync();

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

                Assert.NotNull(ipLocation.Area);
            }

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public async Task GetIpLocationAsyncTest()
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

                Assert.NotNull(ipLocation.Area);
            }

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public async Task MultiThreadingSafeTest()
        {
            var ipSearch = GetInstance();

            var maxTask = 300;

            var ips = Enumerable.Range(0, maxTask).Select(_ => GetRandomIp(ipSearch)).ToList();

            await Parallel.ForEachAsync(ips, async (ip, ParallelLoopState) =>
            {
                var ipLocation = await ipSearch.GetIpLocationAsync(ip);
                Assert.NotNull(ipLocation.Area);
                Assert.True(ipSearch.IpCount > 0);
                Assert.NotNull(ipSearch.Version);
            });
        }

        [Fact]
        public async Task SingleThreadingSafeTest()
        {
            var ipSearch = GetInstance();

            var maxTask = 300;

            var ips = Enumerable.Range(0, maxTask).Select(_ => GetRandomIp(ipSearch)).ToList();

            foreach (var ip in ips)
            {
                var ipLocation = await ipSearch.GetIpLocationAsync(ip);
                Assert.NotNull(ipLocation.Area);
                Assert.True(ipSearch.IpCount > 0);
                Assert.NotNull(ipSearch.Version);
            }
        }
    }
}
