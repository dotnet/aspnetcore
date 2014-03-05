using System;
using System.Diagnostics;
using Microsoft.Net.Runtime;

#if NET45
using Microsoft.Owin.Hosting;

#endif

namespace MvcSample
{
    public class Program
    {
        const string baseUrl = "http://localhost:9001/";
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationEnvironment _env;

        public Program(IServiceProvider serviceProvider,
                       IApplicationEnvironment env)
        {
            _serviceProvider = serviceProvider;
            _env = env;
        }

        public void Main()
        {
#if NET45
            using (WebApp.Start(baseUrl, app => new Startup(_serviceProvider, _env).Configuration(app)))
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