using CerberusLogging.Interfaces.Objects;
using System;

namespace CerbiClientLogging.Classes.ClassTypes
{
    public class ApplicationEntity : IApplicationEntity
    {
        public string ApplicationMessage { get; set; } = String.Empty;
        public string CurrentMethod { get; set; } = string.Empty;
        public IEntityBase? EntityBase { get; set; }
    }
}