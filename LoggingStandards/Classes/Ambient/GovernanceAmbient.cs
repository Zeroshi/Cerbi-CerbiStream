using System.Threading;

namespace CerbiStream.GovernanceRuntime.Ambient;

/// <summary>Ambient flags via AsyncLocal to influence governance evaluation.</summary>
internal static class GovernanceAmbient
{
    private static readonly AsyncLocal<bool> _relaxed = new();
    public static bool IsRelaxed { get => _relaxed.Value; set => _relaxed.Value = value; }
}
