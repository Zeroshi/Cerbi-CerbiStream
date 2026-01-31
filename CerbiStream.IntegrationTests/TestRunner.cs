namespace CerbiStream.IntegrationTests;

/// <summary>
/// Test result tracking for integration tests
/// </summary>
public class TestResult
{
    public string TestName { get; init; } = "";
    public bool Passed { get; init; }
    public string? Message { get; init; }
    public Exception? Exception { get; init; }
    public TimeSpan Duration { get; init; }

    public static TestResult Pass(string name, TimeSpan duration, string? message = null) =>
        new() { TestName = name, Passed = true, Duration = duration, Message = message };

    public static TestResult Fail(string name, TimeSpan duration, string message, Exception? ex = null) =>
        new() { TestName = name, Passed = false, Duration = duration, Message = message, Exception = ex };
}

/// <summary>
/// Test runner for integration tests
/// </summary>
public class TestRunner
{
    private readonly List<TestResult> _results = new();

    public async Task RunTestAsync(string name, Func<Task> test)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await test();
            sw.Stop();
            _results.Add(TestResult.Pass(name, sw.Elapsed));
            Console.WriteLine($"  ✅ {name} ({sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _results.Add(TestResult.Fail(name, sw.Elapsed, ex.Message, ex));
            Console.WriteLine($"  ❌ {name} ({sw.ElapsedMilliseconds}ms)");
            Console.WriteLine($"     Error: {ex.Message}");
        }
    }

    public void RunTest(string name, Action test)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            test();
            sw.Stop();
            _results.Add(TestResult.Pass(name, sw.Elapsed));
            Console.WriteLine($"  ✅ {name} ({sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _results.Add(TestResult.Fail(name, sw.Elapsed, ex.Message, ex));
            Console.WriteLine($"  ❌ {name} ({sw.ElapsedMilliseconds}ms)");
            Console.WriteLine($"     Error: {ex.Message}");
        }
    }

    public (int Passed, int Failed, int Total) GetSummary()
    {
        var passed = _results.Count(r => r.Passed);
        var failed = _results.Count(r => !r.Passed);
        return (passed, failed, _results.Count);
    }

    public IReadOnlyList<TestResult> Results => _results.AsReadOnly();
}
