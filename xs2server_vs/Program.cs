using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// WebSocket服务端
/// </summary>
namespace WebSocketsServer
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer server = new WebSocketServer();
            server.StartServer();
            Console.ReadKey();
        }
    }
}
