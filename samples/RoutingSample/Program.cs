using System;
using Microsoft.Owin.Hosting;

namespace RoutingSample
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "http://localhost:30000";
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Listening on: {0}", url);
                Console.WriteLine("Press ENTER to exit.");
                Console.ReadLine();
            }
        }
    }
}
