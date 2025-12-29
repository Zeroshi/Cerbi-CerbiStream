extern alias GovernanceCore;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Cerbi.Governance;
using CerbiGovernanceModel = GovernanceCore::Cerbi.Governance.CerbiGovernance;
using LogProfile = GovernanceCore::Cerbi.Governance.LogProfile;
using Xunit;

namespace CerbiStream.Tests
{
    public class CerbiStreamGovernanceTests
    {
        private static CerbiGovernanceModel Load(string path)
        {
            if (!File.Exists(path))
            {
                return new CerbiGovernanceModel
                {
                    LoggingProfiles = new Dictionary<string, LogProfile>(StringComparer.OrdinalIgnoreCase)
                };
            }

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new CerbiGovernanceModel
                    {
                        LoggingProfiles = new Dictionary<string, LogProfile>(StringComparer.OrdinalIgnoreCase)
                    };
                }

                return JsonSerializer.Deserialize<CerbiGovernanceModel>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new CerbiGovernanceModel
                {
                    LoggingProfiles = new Dictionary<string, LogProfile>(StringComparer.OrdinalIgnoreCase)
                };
            }
            catch
            {
                return new CerbiGovernanceModel
                {
                    LoggingProfiles = new Dictionary<string, LogProfile>(StringComparer.OrdinalIgnoreCase)
                };
            }
        }

        [Fact]
        public void LoadGovernance_FileDoesNotExist_ReturnsEmptyProfiles()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                var result = Load(path);
                Assert.NotNull(result);
                Assert.Empty(result.LoggingProfiles ?? new Dictionary<string, LogProfile>());
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void LoadGovernance_FileIsValidJson_LoadsProfiles()
        {
            var profile = new CerbiGovernanceModel
            {
                LoggingProfiles = new Dictionary<string, LogProfile>
                {
                    ["SecurityLog"] = new LogProfile
                    {
                        FieldSeverities = new Dictionary<string, string>
                        {
                            ["UserId"] = "Required"
                        }
                    }
                }
            };

            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(profile));

                var result = Load(path);

                Assert.True(result.LoggingProfiles!.ContainsKey("SecurityLog"));
                Assert.Equal("Required", result.LoggingProfiles["SecurityLog"].FieldSeverities["UserId"]);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void LoadGovernance_InvalidJson_ReturnsEmptyProfiles()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path, "{ this is invalid json }");

                var result = Load(path);

                Assert.Empty(result.LoggingProfiles ?? new Dictionary<string, LogProfile>());
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
