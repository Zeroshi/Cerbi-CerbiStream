using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class HttpMessageSender : ISendMessage
{
    private readonly HttpClient _client;
    private readonly string _endpoint;

    public HttpMessageSender(string endpoint, Dictionary<string, string>? headers = null, HttpMessageHandler? handler = null)
    {
        _client = handler != null ? new HttpClient(handler) : new HttpClient();
        foreach (var kvp in headers ?? new()) _client.DefaultRequestHeaders.TryAddWithoutValidation(kvp.Key, kvp.Value);
        _endpoint = endpoint;
    }

    public async Task<bool> SendMessageAsync(string message, string logId)
    {
        var content = new StringContent(message, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(_endpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[CerbiStream] Failed HTTP POST: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        return response.IsSuccessStatusCode;
    }
}

