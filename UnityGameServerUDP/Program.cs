using System;

namespace UnityGameServerUDP
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Run(10, 2, 80, 1, 120, 5002);
        }
    }
}
