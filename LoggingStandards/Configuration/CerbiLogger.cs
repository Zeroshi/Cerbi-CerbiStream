using Cerbi.Governance;
using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public class CerbiLogger
{
    // Using ISendMessage now instead of IQueue
    private readonly ISendMessage? _queue;
    private readonly bool _encryptionEnabled;
    private readonly bool _debugMode;
    private readonly RuntimeGovernanceValidator? _governanceValidator;

    public CerbiLogger(ISendMessage? queue, bool encryptionEnabled, bool debugMode, RuntimeGovernanceValidator? governanceValidator)
    {
        _queue = queue;
        _encryptionEnabled = encryptionEnabled;
        _debugMode = debugMode;
        _governanceValidator = governanceValidator;
    }

    /// <summary>
    /// Logs a message by enriching it with metadata (including a unique LogId) and sending it via the queue.
    /// </summary>
    /// <param name="message">The log message text.</param>
    /// <returns>A Task that indicates whether the log was sent successfully.</returns>
    public async Task<bool> LogAsync(string message)
    {
        if (_debugMode)
        {
            Console.WriteLine($"[DEBUG MODE] Log Message: {message}");
            return true;
        }

        if (_queue == null)
        {
            Console.WriteLine("Logging is not configured.");
            return false;
        }

        // Generate the unique LogId once.
        string logId = Guid.NewGuid().ToString();

        // ✅ Build dictionary to mutate in place
        var logData = new Dictionary<string, object>
        {
            ["LogId"] = logId,
            ["TimestampUtc"] = DateTime.UtcNow,
            ["ApplicationId"] = CerbiStream.Classes.ApplicationMetadata.ApplicationId,
            ["InstanceId"] = CerbiStream.Classes.ApplicationMetadata.InstanceId,
            ["CloudProvider"] = CerbiStream.Classes.ApplicationMetadata.CloudProvider,
            ["Region"] = CerbiStream.Classes.ApplicationMetadata.Region,
            ["Message"] = message
        };

        // ✅ Apply governance if active
        _governanceValidator?.ValidateInPlace(logData);

        // ✅ Serialize after enrichment and validation
        string formattedLog = JsonSerializer.Serialize(logData);
        // Alternatively, if you have a JSON converter:
        // string formattedLog = _jsonConverter.ConvertMessageToJson(enrichedLogEntry);

        // Log to the console (or debug output) using the generated logId.
        Console.WriteLine($"Log with ID {logId} sent to queue.");

        // Send the fully enriched log: pass both the payload and the logId separately.
        return await _queue.SendMessageAsync(formattedLog, logId);
    }
}
