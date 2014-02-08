
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.WebListener;

using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

public class Program
{
    public static void Main(string[] args)
    {
        using (CreateServer(new AppFunc(HelloWorldApp)))
        {
            Console.WriteLine("Running, press enter to exit...");
            Console.ReadLine();
        }
    }

    private static IDisposable CreateServer(AppFunc app)
    {
        IDictionary<string, object> properties = new Dictionary<string, object>();
        IList<IDictionary<string, object>> addresses = new List<IDictionary<string, object>>();
        properties["host.Addresses"] = addresses;

        IDictionary<string, object> address = new Dictionary<string, object>();
        addresses.Add(address);

        address["scheme"] = "http";
        address["host"] = "localhost";
        address["port"] = "8080";
        address["path"] = string.Empty;

        return OwinServerFactory.Create(app, properties);
    }

    public static Task HelloWorldApp(IDictionary<string, object> environment)
    {
            string responseText = "Hello World";
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);

            // See http://owin.org/spec/owin-1.0.0.html for standard environment keys.
            Stream responseStream = (Stream)environment["owin.ResponseBody"];
            IDictionary<string, string[]> responseHeaders =
                (IDictionary<string, string[]>)environment["owin.ResponseHeaders"];

            responseHeaders["Content-Length"] = new string[] { responseBytes.Length.ToString(CultureInfo.InvariantCulture) };
            responseHeaders["Content-Type"] = new string[] { "text/plain" };

            return responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
    }
}
