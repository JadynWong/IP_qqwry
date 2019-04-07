using System;
using System.Collections.Generic;
using System.Text;

namespace QQWry
{
    public class QQWryOptions
    {
        public QQWryOptions()
        {

        }

        public QQWryOptions(string dbPath)
        {
            DbPath = dbPath;
        }

        public string DbPath { get; set; }

        public string CopyWriteUrl { get; set; } = "http://update.cz88.net/ip/copywrite.rar";
        public string QQWryUrl { get; set; } = "http://update.cz88.net/ip/qqwry.rar";
    }
}
