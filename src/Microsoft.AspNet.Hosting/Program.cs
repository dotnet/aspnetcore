using System;
using System.IO;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class Program
    {
        private const string HostingIniFile = "Microsoft.AspNet.Hosting.ini";

        private readonly IServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Main(string[] args)
        {
            var config = new Configuration();
            config.AddCommandLine(args);
            config.AddEnvironmentVariables();
            if (File.Exists(HostingIniFile))
            {
                config.AddIniFile(HostingIniFile);
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices(config));
            var services = serviceCollection.BuildServiceProvider(_serviceProvider);

            var appEnvironment = _serviceProvider.GetService<IApplicationEnvironment>();

            var context = new HostingContext()
            {
                Services = services,
                Configuration = config,
                ServerName = config.Get("server.name"), // TODO: Key names
                ApplicationName = config.Get("app.name")  // TODO: Key names
                    ?? appEnvironment.ApplicationName,
            };

            var engine = services.GetService<IHostingEngine>();
            if (engine == null)
            {
                throw new Exception("TODO: IHostingEngine service not available exception");
            }

            using (engine.Start(context))
            {
                Console.WriteLine("Started");
                Console.ReadLine();
            }
        }
    }
}
