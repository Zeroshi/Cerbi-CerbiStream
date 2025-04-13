using System.Threading.Tasks;

namespace CerbiClientLogging.Interfaces.SendMessage
{
    /// <summary>
    /// Defines the contract for sending a message.
    /// The payload is a complete JSON string (with embedded metadata), and logId is passed as a separate parameter for tracing.
    /// </summary>
    public interface ISendMessage
    {
        Task<bool> SendMessageAsync(string payload, string logId);
    }
}
