using CerberusClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;
using CerberusLogging.Interfaces.Objects;
using Microsoft.Extensions.Logging;
using System;
using static CerberusLogging.Classes.Enums.MetaData;
using Environment = CerberusLogging.Classes.Enums.MetaData.Environment;

namespace CerberusClientLogging.Classes.ClassTypes
{
    /// <summary>
    /// Metadata concerning the message
    /// </summary>
    public class EntityBase : IEntityBase
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public LogLevel LogLevel { get; set; } = LogLevel.None;
        public string? Application_Name { get; set; } = string.Empty;
        public string Log { get; set; } = string.Empty;
        public string? Platform { get; set; } = string.Empty;
        public bool? OnlyInnerException { get; set; } = false;
        public string? Note { get; set; }
        public Exception? Error { get; set; }
        public MetaData.Encryption? Encryption { get; set; }  // ✅ Matches `IEntityBase`
        public MetaData.Environment? Environment { get; set; }  // ✅ Matches `IEntityBase`
        public MetaData.IdentifiableInformation? IdentifiableInformation { get; set; }  // ✅ Matches `IEntityBase`
        public string? Payload { get; set; }
    }
}
