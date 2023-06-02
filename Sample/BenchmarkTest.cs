using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using QQWry;

namespace Sample
{
    [SimpleJob(baseline: true)]
    [RPlotExporter, RankColumn]
    public class BenchmarkTest
    {
        private QQWryIpSearch QQWryIpSearch;

        private QQWryIpSearchMode2 QQWryIpSearchMode2;

        private Java2QQWry Java2QQWry;

        public static QQWryOptions Config;

        [Params(10, 50, 100)]
        public int Range;

        private string[] data;

        [GlobalSetup]
        public void Setup()
        {
            Config = new QQWryOptions()
            {
                DbPath = BenchmarkTest.MapRootPath("qqwry.dat")
            };
            QQWryIpSearchMode2 = new QQWryIpSearchMode2(Config);
            QQWryIpSearch = new QQWryIpSearch(Config);

            //预热
            QQWryIpSearchMode2.GetIpLocation(GetRandomIp(QQWryIpSearchMode2));
            QQWryIpSearch.GetIpLocation(GetRandomIp(QQWryIpSearchMode2));

            Java2QQWry = new Java2QQWry(Config.DbPath);
            Java2QQWry.SearchIPLocation(GetRandomIp(QQWryIpSearchMode2));

            //加载数据
            data = new string[Range];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = GetRandomIp(QQWryIpSearchMode2);
            }
        }

        [Benchmark]
        public string[] QQWryIpSearchExecute()
        {
            return data.Select(x => QQWryIpSearch.GetIpLocation(x).Country).ToArray();
        }

        [Benchmark]
        public string[] QQWryIpSearchMode2Execute()
        {
            return data.Select(x => QQWryIpSearchMode2.GetIpLocation(x).Country).ToArray();
        }

        [Benchmark]
        public string[] Java2QQWryExecute()
        {
            return data.Select(x => Java2QQWry.SearchIPLocation(x).country).ToArray();
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

        public static string GetRandomIp(IIpSearch ipSearch)
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
    }
}
