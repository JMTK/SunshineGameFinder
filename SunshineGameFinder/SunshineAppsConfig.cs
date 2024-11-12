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

    public class PrepCmd
    {
        [JsonPropertyName("do")]
        public string? Do { get; set; }

        [JsonPropertyName("undo")]
        public string? Undo { get; set; }

        [JsonPropertyName("elevated")]
        public string? Elevated { get; set; }
    }

    public static class PathFormatter
    {
        public static string FormatPath(string? path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            // First remove any existing quotes at the start/end
            path = path.Trim('"');

            // Do not normalize steam:// protocol
            if (!path.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
            {
                // Normalize slashes to backslashes
                path = path.Replace("/", "\\");
            }

            // If path has spaces we need to wrap it in quotes
            if (path.Contains(" "))
            {
                return $"\"{path}\"";
            }

            return path;
        }
    }

    public class SunshineApp
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        private string _imagePath = string.Empty;
        [JsonPropertyName("image-path")]
        public string ImagePath
        {
            get => _imagePath;
            set => _imagePath = PathFormatter.FormatPath(value);
        }

        [JsonPropertyName("output")]
        public string? Output { get; set; }

        private string? _cmd;
        [JsonPropertyName("cmd")]
        public string? Cmd
        {
            get => _cmd?.Trim('"');
            set => _cmd = value != null ? PathFormatter.FormatPath(value) : null;
        }

        private string? _workingDir;
        [JsonPropertyName("working-dir")]
        public string? WorkingDir
        {
            get => _workingDir;
            set => _workingDir = value != null ? PathFormatter.FormatPath(value) : null;
        }

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

        private List<string>? _detached;
        [JsonPropertyName("detached")]
        public List<string>? Detached
        {
            get => _detached;
            set => _detached = value?.Select(path => PathFormatter.FormatPath(path)).ToList();
        }
    }

    [JsonConverter(typeof(EnvironmentConverter))]
    public class Env
    {
        private string? path;

        [JsonPropertyName("PATH")]
        public string? Path
        {
            get => path;
            set => path = value != null ? PathFormatter.FormatPath(value) : null;
        }
    }

    public class EnvironmentConverter : JsonConverter<Env>
    {
        public override Env Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return new Env(); // Handle empty string case
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var jsonDoc = JsonDocument.ParseValue(ref reader);
                var root = jsonDoc.RootElement;

                return new Env
                {
                    Path = root.TryGetProperty("PATH", out var pathElement) ? pathElement.GetString() : null
                };
            }

            throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, Env value, JsonSerializerOptions options)
        {
            if (string.IsNullOrEmpty(value.Path))
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            writer.WriteStartObject();
            if (!string.IsNullOrEmpty(value.Path))
            {
                writer.WriteString("PATH", value.Path);
            }
            writer.WriteEndObject();
        }
    }

    public class SunshineConfig
    {
        [JsonPropertyName("env")]
        public Env Env { get; set; } = new Env();

        [JsonPropertyName("apps")]
        public List<SunshineApp>? apps { get; set; }
    }
}