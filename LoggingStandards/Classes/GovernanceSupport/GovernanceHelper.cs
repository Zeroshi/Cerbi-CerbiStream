namespace Cerbi.Governance
{
    /// <summary>
    /// Allows runtime or test code to override the governance config path used by the analyzer.
    /// CerbiStream can call this automatically so the analyzer always picks up the correct config.
    /// </summary>
    public static class GovernanceHelper
    {
        private static string? _overridePath;

        /// <summary>
        /// Set the governance config file path manually.
        /// </summary>
        public static void OverridePath(string path)
        {
            _overridePath = path;
        }

        /// <summary>
        /// Retrieve the overridden governance config path, if set.
        /// </summary>
        public static string? GetOverridePath() => _overridePath;
    }
}
