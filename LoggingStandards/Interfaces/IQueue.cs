using System;
using System.Threading.Tasks;

namespace CerbiClientLogging.Interfaces
{
    public interface IQueue
    {
        Task<bool> SendMessageAsync(string message);
    }

}