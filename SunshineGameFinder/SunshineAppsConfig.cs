using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace SunshineGameFinder
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SunshineConfig))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
    public class SunshineApp
    {
        public string name { get; set; }

        [JsonPropertyName("image-path")]
        public string imagepath { get; set; }
        public List<string>? detached { get; set; }
        public string output { get; set; }
        public string cmd { get; set; }

        [JsonPropertyName("working-dir")]
        public string workingdir { get; set; }

        [JsonPropertyName("exclude-global-prep-cmd")]
        public bool excludeglobalprepcmd { get; set; }
        public bool elevated { get; set; }
        [JsonPropertyName("auto-detach")]
        public bool autodetach { get; set; }
        [JsonPropertyName("wait-all")]
        public bool waitall { get; set; }

        [JsonPropertyName("exit-timeout")]
        public int exittimeout { get; set; } = 5;
    }

    public class SunshineConfig
    {
        public List<SunshineApp>? apps { get; set; }
    }
}
