using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using CerbiStream.Logging.Configuration;

namespace CerbiStream.Tests
{
    public class CerbiStreamGovernanceTests
    {
        [Fact]
        public void LoadGovernance_FileDoesNotExist_ReturnsDefault()
        {
            // Arrange
            var backupPath = "cerbi_governance.json";
            if (File.Exists(backupPath)) File.Move(backupPath, backupPath + ".bak");

            // Act
            var result = CerbiStreamGovernance.LoadGovernance();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.LoggingProfiles);

            // Cleanup
            if (File.Exists(backupPath + ".bak")) File.Move(backupPath + ".bak", backupPath);
        }

        [Fact]
        public void LoadGovernance_FileIsValidJson_LoadsProfiles()
        {
            // Arrange
            var profile = new CerbiStreamGovernance
            {
                LoggingProfiles = new Dictionary<string, LoggingProfile>
                {
                    { "SecurityLog", new LoggingProfile { RequiredFields = new List<string>{"UserId"} } }
                }
            };
            File.WriteAllText("cerbi_governance.json", JsonSerializer.Serialize(profile));

            // Act
            var result = CerbiStreamGovernance.LoadGovernance();

            // Assert
            Assert.True(result.LoggingProfiles.ContainsKey("SecurityLog"));
            Assert.Contains("UserId", result.LoggingProfiles["SecurityLog"].RequiredFields);

            // Cleanup
            File.Delete("cerbi_governance.json");
        }

        [Fact]
        public void IsFieldRequired_WhenProfileAndFieldExist_ReturnsTrue()
        {
            var governance = new CerbiStreamGovernance
            {
                LoggingProfiles = new Dictionary<string, LoggingProfile>
                {
                    { "Audit", new LoggingProfile { RequiredFields = new List<string>{"Action"} } }
                }
            };

            var result = governance.IsFieldRequired("Audit", "Action");

            Assert.True(result);
        }

        [Fact]
        public void IsFieldRequired_WhenProfileDoesNotExist_ReturnsFalse()
        {
            var governance = new CerbiStreamGovernance();
            var result = governance.IsFieldRequired("NonExistent", "Field");
            Assert.False(result);
        }

        [Fact]
        public void IsFieldRequired_WhenFieldDoesNotExist_ReturnsFalse()
        {
            var governance = new CerbiStreamGovernance
            {
                LoggingProfiles = new Dictionary<string, LoggingProfile>
                {
                    { "Audit", new LoggingProfile { RequiredFields = new List<string>{"UserId"} } }
                }
            };

            var result = governance.IsFieldRequired("Audit", "MissingField");

            Assert.False(result);
        }
    }
}
