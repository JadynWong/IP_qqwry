using System.Threading;
using System.Threading.Tasks;
using QQWry;

namespace QQWry
{
    public interface IIpSearch
    {
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
        /// 更新数据库
        /// </summary>
        void UpdateDb();

        /// <summary>
        /// 更新数据库
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task UpdateDbAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// 获取IP信息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> InitAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// 检查是否是IP地址
        /// </summary>
        /// <param name="strIp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IpLocation> GetIpLocationAsync(string strIp, CancellationToken token = default(CancellationToken));
    }
}