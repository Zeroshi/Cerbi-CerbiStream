using CerbiStream.IntegrationTests;
using CerbiStream.IntegrationTests.Tests;

Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         CerbiStream Integration Tests                            ║");
Console.WriteLine("║         Validating all pathways work correctly                   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"Runtime: {Environment.Version}");
Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine();

var runner = new TestRunner();
var sw = System.Diagnostics.Stopwatch.StartNew();

try
{
    // Run all test suites
    await ZeroConfigTests.RunAllAsync(runner);
    await PresetModeTests.RunAllAsync(runner);
    await GovernanceTests.RunAllAsync(runner);
    await EncryptionTests.RunAllAsync(runner);
    await TelemetryTests.RunAllAsync(runner);
    await InstallationScenarioTests.RunAllAsync(runner);
    await EnvironmentConfigTests.RunAllAsync(runner);
}
catch (Exception ex)
{
    Console.WriteLine($"\n💥 Unexpected error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

sw.Stop();

// Print summary
Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════════════════");
var (passed, failed, total) = runner.GetSummary();

if (failed == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ ALL TESTS PASSED: {passed}/{total}");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ TESTS FAILED: {failed}/{total}");
    Console.WriteLine();
    Console.WriteLine("Failed tests:");
    foreach (var result in runner.Results.Where(r => !r.Passed))
    {
        Console.WriteLine($"  - {result.TestName}: {result.Message}");
    }
}

Console.ResetColor();
Console.WriteLine($"Total time: {sw.Elapsed.TotalSeconds:F2}s");
Console.WriteLine("═══════════════════════════════════════════════════════════════════");

// Return exit code based on results
return failed == 0 ? 0 : 1;
