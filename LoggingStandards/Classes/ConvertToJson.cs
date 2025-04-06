using System.Text.Json;
using CerbiStream.Interfaces;
using CerberusLogging.Interfaces.Objects;

namespace CerbiClientLogging.Classes
{
    public class ConvertToJson : IConvertToJson
    {
        public virtual string ConvertMessageToJson<T>(T log)
        {
            return JsonSerializer.Serialize(log);
        }

        public string ConvertApplicationMessageToJson<T>(T log) where T : IApplicationEntity
        {
            return JsonSerializer.Serialize(log);
        }
    }
}
