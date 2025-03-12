using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CerbiClientLogging.Interfaces
{
    public interface IApplicationLogging : IBaseLogging
    {
        // Base version with required information programmatically attainable
        Task<bool> SendApplicationLogAsync(
            string applicationMessage,
            string currentMethod,
            LogLevel logLevel,
            string log,
            HostType hostType,
            DateTime timestamp,
            IEncryption? encryption,
            string operatingSystem,
            string frameworkVersion
        );

        // Extended version with additional common fields
        Task<bool> SendApplicationLogAsync(
            string applicationMessage,
            string currentMethod,
            LogLevel logLevel,
            string log,
            HostType hostType,
            DateTime timestamp,
            IEncryption? encryption,
            string operatingSystem,
            string frameworkVersion,
            string applicationName,
            string platform,
            Exception? error
        );

        // Further extended version with critical fields
        Task<bool> SendApplicationLogAsync(
            string applicationMessage,
            string currentMethod,
            LogLevel logLevel,
            string log,
            HostType hostType,
            DateTime timestamp,
            IEncryption? encryption,
            string operatingSystem,
            string frameworkVersion,
            string applicationName,
            string platform,
            string applicationVersion,
            CloudProvider? cloudProvider,
            IRegion? region,
            Exception? error
        );

        // Full version with all fields
        Task<bool> SendApplicationLogAsync(
            string applicationMessage,
            string currentMethod,
            LogLevel logLevel,
            string log,
            HostType hostType,
            DateTime timestamp,
            IEncryption? encryption,
            string operatingSystem,
            string frameworkVersion,
            string applicationName,
            string platform,
            string applicationVersion,
            CloudProvider? cloudProvider,
            IRegion region,
            string traceId,
            Exception? error,
            ITransactionDestination destination,
            TransactionDestinationTypes destinationType,
            IEnvironment? environment,
            IIdentifiableInformation? identifiableInformation,
            string? organizationId
        );

        // Initialize programmatically attainable data at startup
        static StartupSystemInfo InitializeStartupSystemInfo()
        {
            return new StartupSystemInfo
            {
                OperatingSystem = RuntimeInformation.OSDescription,
                FrameworkVersion = RuntimeInformation.FrameworkDescription,
                HostType = DetectHostType(),
                Timestamp = DateTime.UtcNow
            };
        }

        private static HostType DetectHostType()
        {
            // Implement logic to detect host type based on the environment
            // For now, returning OnPrem as default
            return HostType.OnPrem;
        }
    }

    public interface IEnvironment
    {
        string Name { get; }
        string Description { get; }
    }

    public interface IEncryption
    {
        bool IsEnabled { get; }
        string Encrypt(string input);
    }

    public interface IIdentifiableInformation
    {
        string Identifier { get; }
        string Data { get; }
    }

    public interface ITransactionDestination
    {
        string Name { get; }
        string Type { get; }
        Task SendLogAsync(string log, TransactionDestinationTypes destinationType);
    }

    public interface IRegion
    {
        string RegionName { get; }
        string Country { get; }
    }

    public class StartupSystemInfo
    {
        public string OperatingSystem { get; set; } = string.Empty;
        public string FrameworkVersion { get; set; } = string.Empty;
        public HostType HostType { get; set; } = HostType.WorkStation;
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
    }

    public enum HostType
    {
        OnPrem,
        Cloud,
        WorkStation
    }

    public enum CloudProvider
    {
        Azure,
        AmazonWebServices,
        GoogleCloudPlatform,
        IBMCloud,
        OracleCloud,
        Other
    }

    public enum TransactionDestinationTypes
    {
        Kafka,
        RabbitMQ,
        AzureServiceBus,
        AWS_SQS,
        AWS_Kinesis,
        GooglePubSub,
        Other,
        None
    }
}
