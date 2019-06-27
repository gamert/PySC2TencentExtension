using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace WebSocketsServer
{
    public class Logger
    {
        public bool LogEvents { get; set; }
        public Logger()
        {
            LogEvents = true;
        }
 
        public void Log(string Text)
        {
            if (LogEvents) Console.WriteLine(Text);
        }
    }
}
