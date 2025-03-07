using System;
using System.Threading.Tasks;

namespace CerbiClientLogging.Interfaces.SendMessage
{
    public interface ISendMessage
    {
        Task<bool> SendMessageAsync(string payload, Guid messageId);
    }
}