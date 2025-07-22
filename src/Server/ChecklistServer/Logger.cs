using System;
using System.IO;

namespace ChecklistServer
{
    public static class Logger
    {
        private static readonly object _lock = new();
        public static string LogFilePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RevitChecklist.log");

        public static void Log(string message)
        {
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }

        public static void Log(Exception ex) => Log(ex.ToString());

        public static string ReadAll()
        {
            lock (_lock)
            {
                return File.Exists(LogFilePath) ? File.ReadAllText(LogFilePath) : string.Empty;
            }
        }
    }
}
