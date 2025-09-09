using CerbiStream.GovernanceRuntime.Governance;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging;

public static class CerbiGovernanceRuntimeExtensions
{
    /// <summary>
    /// Wraps the current logging pipeline with governance validation/redaction, writing to <paramref name="innerFactory"/>.
    /// Note: you decide *outside* this library which sinks to register on <paramref name="innerFactory"/>.
    /// </summary>
    public static ILoggingBuilder AddCerbiGovernanceRuntime(
        this ILoggingBuilder builder,
        ILoggerFactory innerFactory,
        string profileName,
        string? configPath = null)
    {
        builder.ClearProviders();
        var adapter = new GovernanceRuntimeAdapter(profileName, configPath);
        builder.AddProvider(new GovernanceLoggerProvider(innerFactory, adapter));
        return builder;
    }
}
