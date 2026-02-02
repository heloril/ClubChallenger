using System.IO;
using System.Text;

namespace NameParser.Infrastructure.Services
{
    public class FileOutputService
    {
        public void WriteToFile(string fileName, string content)
        {
            File.WriteAllText(fileName, content);
        }

        public void AppendToConsoleAndBuilder(StringBuilder builder, string content)
        {
            System.Console.WriteLine(content);
            builder.AppendLine(content);
        }
    }
}
