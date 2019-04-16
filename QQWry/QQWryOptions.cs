using System;
using System.Collections.Generic;
using System.IO;
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

        private string _dbPath = string.Empty;

        /// <summary>
        /// DbPath
        /// </summary>
        public string DbPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_dbPath))
                {
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qqwry.dat");
                }
                else
                {
                    return _dbPath;
                }
            }
            set
            {
                _dbPath = value;
            }
        }

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
