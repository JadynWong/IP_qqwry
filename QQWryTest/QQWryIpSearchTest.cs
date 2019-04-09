using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QQWry;
using Xunit;
// ReSharper disable InconsistentNaming

namespace QQWry.Test
{
    public class QQWryIpSearchTest
    {
        protected QQWryIpSearch GetInstance()
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "qqwry.dat");


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

            var getNewInited = ipSearch.Init(true);

            Assert.True(getNewInited);

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

            var getNewInited = await ipSearch.InitAsync(true);

            Assert.True(getNewInited);

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

                Assert.NotNull(ipLocation.Area);
            }

            Assert.True(ipSearch.IpCount > 0);

            Assert.NotNull(ipSearch.Version);

            ipSearch.Dispose();
        }

        [Fact]
        public void MultiThreadingSafeTest()
        {
            var ipSearch = GetInstance();

            var maxTask = 10000;

            var p = Parallel.For(0, maxTask, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 100
            }, async (num, ParallelLoopState) =>
            {
                var ip = GetRandomIp(ipSearch);
                var ipLocation = await ipSearch.GetIpLocationAsync(ip);
                Assert.NotNull(ipLocation.Area);
                Assert.True(ipSearch.IpCount > 0);
                Assert.NotNull(ipSearch.Version);
            });


        }
    }
}
