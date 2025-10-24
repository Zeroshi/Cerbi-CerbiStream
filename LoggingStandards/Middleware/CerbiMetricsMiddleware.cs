using Microsoft.AspNetCore.Http;
using System.Text;
using System.Threading.Tasks;
using CerbiStream.Observability;

namespace CerbiStream.Middleware
{
 public class CerbiMetricsMiddleware
 {
 private readonly RequestDelegate _next;
 public CerbiMetricsMiddleware(RequestDelegate next) => _next = next;

 public async Task InvokeAsync(HttpContext context)
 {
 var path = context.Request.Path.Value?.TrimEnd('/');
 if (string.Equals(path, "/cerbistream/metrics", System.StringComparison.OrdinalIgnoreCase))
 {
 // Expose simple Prometheus-like metrics
 var sb = new StringBuilder();
 sb.AppendLine("# HELP cerbistream_logs_processed Total logs processed by CerbiStream");
 sb.AppendLine("# TYPE cerbistream_logs_processed counter");
 sb.AppendLine($"cerbistream_logs_processed {Metrics.LogsProcessed}");
 sb.AppendLine("# HELP cerbistream_redactions Total redactions performed");
 sb.AppendLine("# TYPE cerbistream_redactions counter");
 sb.AppendLine($"cerbistream_redactions {Metrics.Redactions}");
 sb.AppendLine("# HELP cerbistream_violations Total governance violations detected");
 sb.AppendLine("# TYPE cerbistream_violations counter");
 sb.AppendLine($"cerbistream_violations {Metrics.Violations}");

 context.Response.ContentType = "text/plain; version=0.0.4";
 await context.Response.WriteAsync(sb.ToString());
 return;
 }
 if (string.Equals(path, "/cerbistream/health", System.StringComparison.OrdinalIgnoreCase))
 {
 context.Response.ContentType = "application/json";
 await context.Response.WriteAsync("{\"status\":\"Healthy\"}");
 return;
 }

 await _next(context);
 }
 }
}
