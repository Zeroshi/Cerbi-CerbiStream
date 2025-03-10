using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.Threading.Tasks;

namespace CerbiClientLogging.Interfaces
{
    public interface IQueue
    {
        Task<bool> SendMessageAsync(string message, Guid messageId);
    }

}