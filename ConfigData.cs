using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace RevoluteConfigApp
{
    public class ConfigData
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public byte[] LeftReport { get; set; }
        public byte[] RightReport { get; set; }
        public string LeftTransport { get; set; } // New property for LeftTransport
        public string RightTransport { get; set; } // New property for RightTransport
    }
}
