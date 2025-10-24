using System;
using System.Collections.Generic;
using System.Text.Json;
using CerberusLogging.Interfaces.Objects;

namespace CerbiClientLogging.Interfaces
{
    public interface ILogMessageDecoder
    {
        T DecodeLogMessage<T>(string jsonLog) where T : IEntityBase;
        Dictionary<string, object> ExtractMetadata(string jsonLog);
    }

    public class LogMessageDecoder : ILogMessageDecoder
    {
        public T DecodeLogMessage<T>(string jsonLog) where T : IEntityBase
        {
            var result = JsonSerializer.Deserialize<T>(jsonLog, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? throw new InvalidOperationException("Invalid log format.");
        }

        public Dictionary<string, object> ExtractMetadata(string jsonLog)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonLog);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return new Dictionary<string, object>();

                if (!doc.RootElement.TryGetProperty("Metadata", out var meta) || meta.ValueKind != JsonValueKind.Object)
                    return new Dictionary<string, object>();

                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in meta.EnumerateObject())
                {
                    dict[p.Name] = p.Value.ValueKind switch
                    {
                        JsonValueKind.String => (object)(p.Value.GetString()!),
                        JsonValueKind.Number => p.Value.TryGetInt64(out var i) ? i : p.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null or JsonValueKind.Undefined => null!,
                        _ => p.Value.ToString()
                    };
                }
                return dict;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
    }
}
