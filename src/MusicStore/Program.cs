using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.Hosting;
using System;
using System.Threading.Tasks;

namespace MusicStore
{
    /// <summary>
    /// This demonstrates how the application can be launched in a K console application. 
    /// k run command in the application folder will invoke this.
    /// </summary>
    public class Program
    {
        private readonly IServiceProvider _hostServiceProvider;

        public Program(IServiceProvider hostServiceProvider)
        {
            _hostServiceProvider = hostServiceProvider;
        }

        public Task<int> Main(string[] args)
        {
            //Add command line configuration source to read command line parameters.
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
                ApplicationName = "MusicStore"
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