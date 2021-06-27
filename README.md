# IP_qqwry [纯真IP数据库](http://www.cz88.net/)操作

[![.NET Core](https://github.com/JadynWong/IP_qqwry/actions/workflows/dotnetcore.yml/badge.svg)](https://github.com/JadynWong/IP_qqwry/actions/workflows/dotnetcore.yml)
[![release](https://github.com/JadynWong/IP_qqwry/actions/workflows/release.yml/badge.svg)](https://github.com/JadynWong/IP_qqwry/actions/workflows/release.yml)

|  Packages   | Version  |
|  ----  | ----  |
|QQWry   | [![NuGet](https://img.shields.io/nuget/v/QQWry.svg?style=flat)](https://www.nuget.org/packages/QQWry) |
| QQWry.DependencyInjection | [![NuGet](https://img.shields.io/nuget/v/QQWry.DependencyInjection.svg?style=flat)](https://www.nuget.org/packages/QQWry.DependencyInjection) |

支持在线更新数据库

## QQWry

```csharp
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
```

## QQWry.DependencyInjection

```csharp
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
```

## IIpSearch

```csharp
/// <summary>
/// 数据库IP数量
/// </summary>
int IpCount { get; }

/// <summary>
/// 数据库版本
/// </summary>
string Version { get; }

/// <summary>
/// 检查是否是IP地址
/// </summary>
/// <param name="ip"></param>
/// <returns></returns>
bool CheckIp(string ip);

/// <summary>
/// 获取IP信息
/// </summary>
/// <param name="strIp"></param>
/// <returns></returns>
IpLocation GetIpLocation(string strIp);

/// <summary>
/// 检查是否是IP地址
/// </summary>
/// <param name="strIp"></param>
/// <param name="token"></param>
/// <returns></returns>
Task<IpLocation> GetIpLocationAsync(string strIp, CancellationToken token = default(CancellationToken));

/// <summary>
/// 获取QQWry CopyWrite
/// </summary>
/// <returns></returns>
QQWryCopyWrite GetCopyWrite();

/// <summary>
/// 获取QQWry CopyWrite
/// </summary>
/// <returns></returns>
Task<QQWryCopyWrite> GetCopyWriteAsync();

/// <summary>
/// 获取IP信息
/// </summary>
/// <param name="getNewDb">获取新数据库</param>
/// <returns></returns>
bool Init(bool getNewDb = false);

/// <summary>
/// 获取IP信息
/// </summary>
/// <param name="getNewDb"></param>
/// <param name="token"></param>
/// <returns></returns>
Task<bool> InitAsync(bool getNewDb = false, CancellationToken token = default(CancellationToken));
```
