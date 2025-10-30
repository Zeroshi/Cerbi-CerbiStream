using BenchmarkDotNet.Running;

namespace BenchmarkSuite1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Run all benchmarks in this assembly with shorter default job if no args supplied
            if (args is null || args.Length ==0)
            {
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(new[] { "--job", "Short", "--warmupCount", "1", "--iterationCount", "6" });
            }
            else
            {
                var _ = BenchmarkRunner.Run(typeof(Program).Assembly);
            }
        }
    }
}
