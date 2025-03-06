using CerberusLogging.Interfaces.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CerberusClientLogging.Interfaces
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
            return JsonConvert.DeserializeObject<T>(jsonLog) ?? throw new InvalidOperationException("Invalid log format.");
        }

        public Dictionary<string, object> ExtractMetadata(string jsonLog)
        {
            var logEntry = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonLog);
            return logEntry != null && logEntry.ContainsKey("Metadata")
                ? (Dictionary<string, object>)logEntry["Metadata"]
                : new Dictionary<string, object>();
        }
    }
}
