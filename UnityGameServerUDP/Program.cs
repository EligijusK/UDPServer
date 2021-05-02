using System;

namespace UnityGameServerUDP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Server server = new Server();
            server.Run(10, 120, 5002);
        }
    }
}
