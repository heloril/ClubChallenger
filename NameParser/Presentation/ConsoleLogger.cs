using System;
using System.Text;

namespace NameParser.Presentation
{
    public class ConsoleLogger
    {
        private readonly StringBuilder _log;

        public ConsoleLogger()
        {
            _log = new StringBuilder();
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
            _log.AppendLine(message);
        }

        public string GetLog()
        {
            return _log.ToString();
        }
    }
}
