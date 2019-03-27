# IP_qqwry 纯真IP数据库操作

支持在线更新数据库

`

    var config = new QQWryOptions()
    {
        DbPath = MapRootPath("~/IP/qqwry.dat")
    };

    var ipSearch = new QQWryIpSearch(config);

    foreach (var ip in preSearchIpArray)
    {
        var ipLocation = ipSearch.GetIpLocation(ip);
        Write(ipLocation);
    }
    Console.WriteLine("记录总数" + ipSearch.IpCount);
    Console.WriteLine("版本" + ipSearch.Version);
`
