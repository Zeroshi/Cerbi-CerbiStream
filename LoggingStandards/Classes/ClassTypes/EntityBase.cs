using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;
using System;

namespace CerbiClientLogging.Classes.ClassTypes
{
    /// <summary>
    /// Metadata concerning the message
    /// </summary>
    public class EntityBase
    {
        public Guid MessageId { get; set; }
        public DateTime TimeStamp { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Application_Name { get; set; }
        public string Log { get; set; }
        public string Platform { get; set; }
        public bool OnlyInnerException { get; set; }
        public string Note { get; set; }
        public Exception? Error { get; set; }
        public MetaData.Encryption Encryption { get; set; }
        public MetaData.Environment Environment { get; set; }
        public MetaData.IdentifiableInformation? IdentifiableInformation { get; set; }
        public string Payload { get; set; }
        public string CloudProvider { get; set; } = "Unknown";
        public string InstanceId { get; set; } = "Unknown";
        public string ApplicationVersion { get; set; } = "Unknown";
        public string Region { get; set; } = "Unknown";
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }

}
