using Amazon;

namespace CerbiStream.GovernanceRuntime.Providers;

public interface IGovernanceProfileProvider
{
    Profile GetActiveProfile();
    string AppName { get; }
}
