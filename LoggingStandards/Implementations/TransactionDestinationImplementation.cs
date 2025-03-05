using System;
using System.Threading.Tasks;
using CerberusClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;

namespace CerberusClientLogging.Implementations
{
    public class TransactionDestinationImplementation : ITransactionDestination
    {
        public string Name => "DefaultTransaction";
        public string Type => "DefaultType";

        public Task SendLogAsync(string log, TransactionDestinationTypes destinationType)
        {
            Console.WriteLine($"Log sent to {Type}: {log}");
            return Task.CompletedTask;
        }
    }
}
