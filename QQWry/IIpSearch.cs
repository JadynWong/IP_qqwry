using QQWry;

namespace QQWry
{
    public interface IIpSearch
    {
        int IpCount { get; }

        string Version { get; }

        bool CheckIp(string ip);

        IpLocation GetIpLocation(string strIp);

        void UpdateDb();
    }
}