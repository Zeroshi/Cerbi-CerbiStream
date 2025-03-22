// 🔹 TelemetryContext.cs

using System.Collections.Generic;

namespace CerbiStream.Telemetry
{
    public static class TelemetryContext
    {
        public static string? ServiceName { get; set; }
        public static string? OriginApp { get; set; }
        public static string? UserType { get; set; }
        public static string? Feature { get; set; }
        public static bool? IsRetry { get; set; }
        public static int? RetryAttempt { get; set; }

        public static Dictionary<string, object> Snapshot()
        {
            var result = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(ServiceName)) result["ServiceName"] = ServiceName;
            if (!string.IsNullOrWhiteSpace(OriginApp)) result["OriginApp"] = OriginApp;
            if (!string.IsNullOrWhiteSpace(UserType)) result["UserType"] = UserType;
            if (!string.IsNullOrWhiteSpace(Feature)) result["Feature"] = Feature;

            result["IsRetry"] = IsRetry ?? false;
            result["RetryAttempt"] = RetryAttempt ?? 0;

            return result;
        }
    }
}
