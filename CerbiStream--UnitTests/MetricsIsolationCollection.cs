using CerbiStream.Observability;
using Xunit;

namespace CerbiStream.Tests
{
    [CollectionDefinition("MetricsIsolation", DisableParallelization = true)]
    public class MetricsIsolationCollection : ICollectionFixture<MetricsIsolationFixture>
    {
    }

    public class MetricsIsolationFixture : IDisposable
    {
        public MetricsIsolationFixture()
        {
            Metrics.Reset();
        }

        public void Dispose()
        {
            Metrics.Reset();
        }
    }
}
