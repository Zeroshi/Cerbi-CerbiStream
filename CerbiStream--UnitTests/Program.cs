using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerberusClientLogging.Implementations;
using CerberusClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;
using CerberusClientLogging.Classes;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Starting manual test for Logging...");

        // ✅ Correct setup of Dependency Injection
        var serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSimpleConsole(options =>  // ✅ This is the new method for .NET 8+
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                });
            })
            .AddSingleton<ITransactionDestination, TransactionDestinationImplementation>()  // Replace with your actual implementation
            .AddSingleton<ConvertToJson>()
            .BuildServiceProvider();

        // ✅ Resolve dependencies
        var logger = serviceProvider.GetRequiredService<ILogger<Logging>>();
        var transactionDestination = serviceProvider.GetRequiredService<ITransactionDestination>();
        var jsonConverter = serviceProvider.GetRequiredService<ConvertToJson>();

        // ✅ Create Logging Instance
        var logging = new Logging(
            serviceProvider.GetRequiredService<ILogger<Logging>>(),
            serviceProvider.GetRequiredService<ITransactionDestination>(),
            serviceProvider.GetRequiredService<ConvertToJson>(),
            serviceProvider.GetRequiredService<IEncryption>()  // ✅ Now correctly passing encryption
        );


        // ✅ Execute Log Test with all required parameters
        bool result = await logging.SendApplicationLogAsync(
            applicationMessage: "Test log message",
            currentMethod: "Main",
            logLevel: LogLevel.Information,
            log: "Test log entry",
            applicationName: "Test Console App",
            platform: "Windows",
            onlyInnerException: false,
            note: "Manual test note",
            error: null,
            transactionDestination: transactionDestination,
            transactionDestinationTypes: TransactionDestinationTypes.Other,
            encryption: null,
            environment: null,
            identifiableInformation: null,
            payload: "Sample payload",
            cloudProvider: "Azure",               // ✅ REQUIRED
            instanceId: "Instance-12345",         // ✅ REQUIRED
            applicationVersion: "v1.0.0",         // ✅ REQUIRED
            region: "US-East",                    // ✅ REQUIRED
            requestId: "Request-ABC123"           // ✅ REQUIRED
        );

        Console.WriteLine($"Logging test result: {result}");
    }
}
