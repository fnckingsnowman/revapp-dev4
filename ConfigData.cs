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

        [JsonPropertyName("LeftReport")]
        public byte[] LeftReport { get; set; }

        [JsonPropertyName("RightReport")]
        public byte[] RightReport { get; set; }
    }
}
