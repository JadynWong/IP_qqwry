using System;
using System.Collections.Generic;
using System.Text;
// ReSharper disable InconsistentNaming

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

        /// <summary>
        /// DbPath
        /// </summary>
        public string DbPath { get; set; }

        /// <summary>
        /// CopyWriteUrl
        /// </summary>
        public string CopyWriteUrl { get; set; } = "http://update.cz88.net/ip/copywrite.rar";

        /// <summary>
        /// QQWryUrl
        /// </summary>
        public string QQWryUrl { get; set; } = "http://update.cz88.net/ip/qqwry.rar";
    }
}
