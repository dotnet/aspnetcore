
using System;
using System.Text;
using Microsoft.Net.Server;

namespace HelloWorld
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (WebListener listener = new WebListener())
            {
                listener.UrlPrefixes.Add(UrlPrefix.Create("http://localhost:8080"));
                listener.Start();

                Console.WriteLine("Running...");
                while (true)
                {
                    RequestContext context = listener.GetContextAsync().Result;
                    Console.WriteLine("Accepted");

                    // Context:
                    // context.User;
                    // context.DisconnectToken
                    // context.Dispose()
                    // context.Abort();

                    // Request
                    // context.Request.ProtocolVersion
                    // context.Request.IsLocal
                    // context.Request.Headers // TODO: Header helpers?
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

                    context.Response.ContentLength = bytes.Length;
                    context.Response.ContentType = "text/plain";

                    context.Response.Body.Write(bytes, 0, bytes.Length);
                    context.Dispose();
                }
            }
        }
    }
}