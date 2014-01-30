#if NET45
using System;
using Microsoft.Owin.Hosting;

namespace MvcSample
{
    public class Program
    {
        const string baseUrl = "http://localhost:9001/";

        public static void Main()
        {
            using (WebApp.Start<Startup>(new StartOptions(baseUrl)))
            {
                Console.WriteLine("Listening at {0}", baseUrl);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }
    }
}
#endif