using AspNetCoreModule.Test.Framework;
using AspNetCoreModule.Test.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClientEXE
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
            {
                if (args.Length == 0)
                {
                    TestUtility.LogInformation("Usage: WebSocketClientEXE http://localhost:40000/aspnetcoreapp/websocket");
                    return;
                }
                string url = "http://localhost:40000/aspnetcoreapp/websocket";
                if (args[0].Contains("http:"))
                {
                    url = args[0];
                }
                var frameReturned = websocketClient.Connect(new Uri(url), true, true);
                TestUtility.LogInformation(frameReturned.Content);
                TestUtility.LogInformation("Type any data and Enter key ('Q' to quit): ");

                while (true)
                {
                    Thread.Sleep(500);
                    if (!websocketClient.IsOpened)
                    {
                        TestUtility.LogInformation("Connection closed...");
                        break;
                    }
                    
                    string data = Console.ReadLine();
                    if (data.Trim().ToLower() == "q")
                    {
                        frameReturned = websocketClient.Close();
                        TestUtility.LogInformation(frameReturned.Content);
                        break;
                    }
                    websocketClient.SendTextData(data);
                }
            }
        }
    }
}
