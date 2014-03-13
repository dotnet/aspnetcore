using System;
#if NET45
using System.Diagnostics;
using Microsoft.Owin.Hosting;
#endif

namespace WelcomePageSample
{
    public class Program
    {
        const string baseUrl = "http://localhost:9001/";

        public static void Main()
        {
#if NET45
            using (WebApp.Start<Startup>(new StartOptions(baseUrl)))
            {
                Console.WriteLine("Listening at {0}", baseUrl);
                Process.Start(baseUrl);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
#else
            Console.WriteLine("Hello World");
#endif
        }
    }
}