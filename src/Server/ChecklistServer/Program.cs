using System;

namespace ChecklistServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var server = new Server("http://localhost:51789/");
            server.Start();
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
