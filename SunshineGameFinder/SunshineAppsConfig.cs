using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace SunshineGameFinder
{
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(SunshineConfig))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class PrepCmd
    {
        [JsonPropertyName("do")]
        public string? Do { get; set; }

        [JsonPropertyName("undo")]
        public string? Undo { get; set; }

        [JsonPropertyName("elevated")]
        public string? Elevated { get; set; }
    }

    public class SunshineApp
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("image-path")]
        public string ImagePath { get; set; }

        [JsonPropertyName("output")]
        public string? Output { get; set; }

        [JsonPropertyName("cmd")]
        public string? Cmd { get; set; }

        [JsonPropertyName("working-dir")]
        public string? WorkingDir { get; set; }

        [JsonPropertyName("exclude-global-prep-cmd")]
        public string? ExcludeGlobalPrepCmd { get; set; }

        [JsonPropertyName("elevated")]
        public string? Elevated { get; set; }

        [JsonPropertyName("auto-detach")]
        public string? AutoDetach { get; set; }

        [JsonPropertyName("wait-all")]
        public string? WaitAll { get; set; }

        [JsonPropertyName("exit-timeout")]
        public string? ExitTimeout { get; set; }

        [JsonPropertyName("prep-cmd")]
        public List<PrepCmd>? PrepCmd { get; set; }

        [JsonPropertyName("detached")]
        public List<string>? Detached { get; set; }
    }

    public class SunshineConfig
    {
        public List<SunshineApp> apps { get; set; }
    }
}
