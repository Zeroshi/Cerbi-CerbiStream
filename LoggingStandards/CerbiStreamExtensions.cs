using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiStream.Configuration;
using CerbiStream.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CerbiStream
{
    public static class CerbiStreamExtensions
    {
        public static IServiceCollection UseCerbiStream(this IServiceCollection services, CerbiStreamConfig config)
        {
            return services
                .AddSingleton(config) // ✅ Store the config object in DI
                .AddSingleton<IQueue>(provider => QueueFactory.CreateQueue(config)) // ✅ Create queue based on config
                .AddSingleton<IConvertToJson, ConvertToJson>()
                .AddSingleton<IEncryption, EncryptionImplementation>()
                .AddSingleton<IBaseLogging, Logging>();
        }
    }
}
