using CerbiClientLogging.Interfaces;
using System;
using System.Threading.Tasks;

public class CerbiLogger
{
    private readonly IQueue? _queue;
    private readonly bool _encryptionEnabled;
    private readonly bool _debugMode;

    public CerbiLogger(IQueue? queue, bool encryptionEnabled, bool debugMode)
    {
        _queue = queue;
        _encryptionEnabled = encryptionEnabled;
        _debugMode = debugMode;
    }

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

        return await _queue.SendMessageAsync(message, Guid.NewGuid());
    }
}
