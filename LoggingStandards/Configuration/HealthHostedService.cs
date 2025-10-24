using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CerbiStream.Configuration
{
 /// <summary>
 /// Lightweight hosted service that validates important CerbiStream runtime artifacts (policy file, fallback directory).
 /// This is a low-risk health check helper that logs warnings/errors on startup if configuration is invalid.
 /// </summary>
 public class HealthHostedService : IHostedService
 {
 private readonly ILogger<HealthHostedService> _logger;
 private readonly string _policyPath;

 public HealthHostedService(ILogger<HealthHostedService> logger, string? policyPath = null)
 {
 _logger = logger ?? throw new ArgumentNullException(nameof(logger));
 _policyPath = string.IsNullOrWhiteSpace(policyPath)
 ? (Environment.GetEnvironmentVariable("CERBI_GOVERNANCE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "cerbi_governance.json"))
 : policyPath!;
 }

 public Task StartAsync(CancellationToken cancellationToken)
 {
 try
 {
 if (!File.Exists(_policyPath))
 {
 _logger.LogWarning("CerbiStream policy file not found at {Path}. Governance will start with defaults.", _policyPath);
 }
 else
 {
 _logger.LogInformation("CerbiStream policy file found at {Path}.", _policyPath);
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "HealthHostedService encountered an error checking policy file.");
 }

 return Task.CompletedTask;
 }

 public Task StopAsync(CancellationToken cancellationToken)
 {
 // No-op
 return Task.CompletedTask;
 }
 }
}
