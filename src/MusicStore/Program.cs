using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.ConfigurationModel.Sources;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Hosting;
using System;
using System.Threading.Tasks;

namespace MusicStore
{
    public class Program
    {
        private readonly IServiceProvider _hostServiceProvider;

        public Program(IServiceProvider hostServiceProvider)
        {
            _hostServiceProvider = hostServiceProvider;
        }

        public Task<int> Main(string[] args)
        {
            //Add command line to the configuration source to read command line parameters.
            var config = new Configuration();
            config.AddCommandLine(args);
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices(config));
            var services = serviceCollection.BuildServiceProvider(_hostServiceProvider);

            var context = new HostingContext()
            {
                Services = services,
                Configuration = config,
                ServerName = "Microsoft.AspNet.Server.WebListener",
                ApplicationName = "BugTracker"
            };

            var engine = services.GetService<IHostingEngine>();
            if (engine == null)
            {
                throw new Exception("TODO: IHostingEngine service not available exception");
            }

            using (engine.Start(context))
            {
                Console.WriteLine("Started the server..");
                Console.WriteLine("Press any key to stop the server");
                Console.ReadLine();
            }
            return Task.FromResult(0);
        }
    }
}