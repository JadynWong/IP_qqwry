using System;
using System.Collections.Generic;
using System.Text;

namespace QQWry
{
    public class QQWryCopyWrite
    {
        public string Sign { get; set; }
        public UInt32 Version { get; set; }
        public UInt32 Unknown1 { get; set; }
        public UInt32 Size { get; set; }
        public UInt32 Unknown2 { get; set; }
        public UInt32 Key { get; set; }
        public string Text { get; set; }
        public string Link { get; set; }
    }
}
