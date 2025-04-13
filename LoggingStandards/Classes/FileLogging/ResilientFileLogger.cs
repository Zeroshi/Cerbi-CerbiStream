using System;
using System.Text.Json;
using System.Threading;

public class ResilientFileLogger
{
    private readonly string _primaryPath;
    private readonly string _fallbackPath;
    private readonly int _retryCount;
    private readonly TimeSpan _retryDelay;
    private readonly IFileWriter _fileWriter;

    public ResilientFileLogger(string primaryPath, string fallbackPath, int retryCount, TimeSpan retryDelay, IFileWriter fileWriter)
    {
        _primaryPath = primaryPath;
        _fallbackPath = fallbackPath;
        _retryCount = retryCount;
        _retryDelay = retryDelay;
        _fileWriter = fileWriter;
    }

    public void Log(object logEntry)
    {
        var json = JsonSerializer.Serialize(logEntry);
        for (int i = 0; i < _retryCount; i++)
        {
            try
            {
                _fileWriter.AppendText(_primaryPath, json + Environment.NewLine);
                return;
            }
            catch
            {
                Thread.Sleep(_retryDelay);
            }
        }

        try
        {
            _fileWriter.AppendText(_fallbackPath, json + Environment.NewLine);
        }
        catch
        {
            // Final fail-silent fallback.
        }
    }
}
