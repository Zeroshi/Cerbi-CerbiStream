using System.Threading.Tasks;

namespace CerbiStream.Interfaces
{
    /// <summary>
    /// Defines the operations required for a queue client.
    /// </summary>
    public interface IQueueClient
    {
        /// <summary>
        /// Sends the log payload asynchronously to the queue.
        /// The payload includes the log message and any metadata (GUID, ApplicationId, etc.).
        /// </summary>
        /// <param name="payload">The JSON payload including all metadata.</param>
        /// <returns>A task with a boolean indicating success.</returns>
        Task<bool> SendMessageAsync(string payload);
    }
}
