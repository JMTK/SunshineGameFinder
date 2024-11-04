namespace SunshineGameFinder
{
    internal class Logger
    {
        public static void Log(string message)
        {
            Log(message, LogLevel.Information);
        }

        public static void Log(string message, bool newline)
        {
            Log(message, LogLevel.Information, newline);
        }

        public static void Log(string message, LogLevel level, bool newline = true)
        {
            switch (level)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogLevel.Trace:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            if (newline)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    internal enum LogLevel
    {
        Error,
        Warning,
        Information,
        Success,
        Trace
    }

}
