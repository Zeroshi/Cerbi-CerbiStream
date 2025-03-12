using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CerbiStream.Logging.Configuration
{
    public class CerbiStreamGovernance
    {
        private static readonly string _governanceFilePath = "cerbi_governance.json";

        public Dictionary<string, LoggingProfile> LoggingProfiles { get; set; } = new();

        public static CerbiStreamGovernance LoadGovernance()
        {
            if (!File.Exists(_governanceFilePath))
                return new CerbiStreamGovernance(); // Default: No enforcement

            try
            {
                var json = File.ReadAllText(_governanceFilePath);
                return JsonConvert.DeserializeObject<CerbiStreamGovernance>(json) ?? new CerbiStreamGovernance();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cerbi Governance] Error loading governance: {ex.Message}");
                return new CerbiStreamGovernance();
            }
        }

        public bool IsFieldRequired(string profileName, string field)
        {
            if (!LoggingProfiles.ContainsKey(profileName)) return false;
            return LoggingProfiles[profileName].RequiredFields.Contains(field);
        }
    }

    public class LoggingProfile
    {
        public List<string> RequiredFields { get; set; } = new();
        public List<string> OptionalFields { get; set; } = new();
    }
}

