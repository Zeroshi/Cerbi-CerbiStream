namespace CerbiStream.Interfaces
{
    public interface IConvertToJson
    {
        string ConvertMessageToJson<T>(T log);
    }

}
