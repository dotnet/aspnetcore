using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.ConfigurationModel;

namespace MusicStore
{
    /// <summary>
    /// This demonstrates how the application can be launched in a console application. 
    /// "dnx . run" command in the application folder will invoke this.
    /// </summary>
    public class Program
    {
        public Task<int> Main(string[] args)
        {
            //Add command line configuration source to read command line parameters.
            var config = new Configuration();
            config.AddCommandLine(args);

            using (WebHost.CreateEngine(config)
                .UseServer("Microsoft.AspNet.Server.WebListener")
                .Start())
            {
                Console.WriteLine("Started the server..");
                Console.WriteLine("Press any key to stop the server");
                Console.ReadLine();
            }
            return Task.FromResult(0);
        }
    }
}