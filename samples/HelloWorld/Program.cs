using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Net.Http.Server;

namespace HelloWorld
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Run(args).Wait();
        }
        
        public static async Task Run(string[] args)
        {
            var settings = new WebListenerSettings();
            settings.UrlPrefixes.Add("http://localhost:8080");

            using (WebListener listener = new WebListener(settings))
            {
                listener.Start();

                Console.WriteLine("Running...");
                while (true)
                {
                    RequestContext context = await listener.AcceptAsync();
                    Console.WriteLine("Accepted");

                    // Context:
                    // context.User;
                    // context.DisconnectToken
                    // context.Dispose()
                    // context.Abort();

                    // Request
                    // context.Request.ProtocolVersion
                    // context.Request.Headers
                    // context.Request.Method
                    // context.Request.Body
                    // Content-Length - long?
                    // Content-Type - string
                    // IsSecureConnection
                    // HasEntityBody

                    // TODO: Request fields
                    // Content-Encoding - Encoding
                    // Host
                    // Client certs - GetCertAsync, CertErrors
                    // Cookies
                    // KeepAlive
                    // QueryString (parsed)
                    // RequestTraceIdentifier
                    // RawUrl
                    // URI
                    // IsWebSocketRequest
                    // LocalEndpoint vs LocalIP & LocalPort
                    // RemoteEndpoint vs RemoteIP & RemotePort
                    // AcceptTypes string[]
                    // ServiceName
                    // TransportContext

                    // Response
                    byte[] bytes = Encoding.ASCII.GetBytes("Hello World: " + DateTime.Now);

                    if (context.IsWebSocketRequest)
                    {
                        Console.WriteLine("WebSocket");
                        WebSocket webSocket = await context.AcceptWebSocketAsync();
                        await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
                        webSocket.Dispose();
                    }
                    else
                    {
                        Console.WriteLine("Hello World");
                        context.Response.ContentLength = bytes.Length;
                        context.Response.ContentType = "text/plain";

                        context.Response.Body.Write(bytes, 0, bytes.Length);
                        context.Dispose();
                    }
                }
            }
        }
    }
}