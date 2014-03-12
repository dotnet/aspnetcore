using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.ConfigurationModel.Sources;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Net.Runtime;
using System;
using System.IO;

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

            var services = new ServiceProvider(_serviceProvider)
                .Add(HostingServices.GetDefaultServices(config));

            var appEnvironment = _serviceProvider.GetService<IApplicationEnvironment>();

            var context = new HostingContext()
            {
                Services = services,
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
#if NET45
                Console.WriteLine("Started");
                Console.ReadLine();
#endif
            }
        }
    }
}
