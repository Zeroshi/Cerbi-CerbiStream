using Polly;
using Polly.Retry;
using System;

public static class RetryPolicyFactory
{
    public static AsyncRetryPolicy Create(int retries, int delayMs) =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(retries, retryAttempt =>
                TimeSpan.FromMilliseconds(delayMs),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"[Retry] Attempt {retryCount} failed: {exception.Message}");
                });
}
