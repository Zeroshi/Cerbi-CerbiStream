using System.IO;

public class FileWriter : IFileWriter
{
    public void AppendText(string path, string contents) =>
        File.AppendAllText(path, contents);
}
