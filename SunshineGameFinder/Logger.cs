namespace SunshineGameFinder
{
    internal class Logger
    {
        public static void Log(string message)
        {
            Log(message, LogLevel.Information);
        }

        public static void Log(string message, LogLevel level)
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
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            
        }
    }

    internal enum LogLevel
    {
        Error,
        Warning,
        Information,
        Success
    }

}
