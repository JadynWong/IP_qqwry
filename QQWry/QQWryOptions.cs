using System;
using System.IO;

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
    }
}
