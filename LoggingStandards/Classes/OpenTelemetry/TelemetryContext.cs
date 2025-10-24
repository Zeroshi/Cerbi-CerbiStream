using System.Collections.Generic;
using System.Threading;

namespace CerbiStream.Telemetry
{
    public static class TelemetryContext
    {
        // Global/static attributes set at startup
        public static string? ServiceName { get; set; }
        public static string? OriginApp { get; set; }
        public static string? UserType { get; set; }
        public static string? Feature { get; set; }

        // Request-scoped context stored in AsyncLocal
        private static readonly AsyncLocal<Dictionary<string, object>?> _requestContext = new();

        public static void Set(string key, object? value)
        {
            var dict = _requestContext.Value;
            if (dict == null)
            {
                dict = new Dictionary<string, object>();
                _requestContext.Value = dict;
            }
            if (value == null)
                dict.Remove(key);
            else
                dict[key] = value!;
        }

        public static void ClearRequest()
        {
            _requestContext.Value = null;
        }

        public static Dictionary<string, object> Snapshot()
        {
            var result = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(ServiceName)) result["ServiceName"] = ServiceName!;
            if (!string.IsNullOrWhiteSpace(OriginApp)) result["OriginApp"] = OriginApp!;
            if (!string.IsNullOrWhiteSpace(UserType)) result["UserType"] = UserType!;
            if (!string.IsNullOrWhiteSpace(Feature)) result["Feature"] = Feature!;

            var req = _requestContext.Value;
            if (req != null)
            {
                foreach (var kv in req)
                {
                    if (!result.ContainsKey(kv.Key)) result[kv.Key] = kv.Value;
                }
            }

            return result;
        }

        public static void Clear()
        {
            ServiceName = null;
            OriginApp = null;
            UserType = null;
            Feature = null;
            ClearRequest();
        }
    }
}
