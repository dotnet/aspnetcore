using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace TestServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) => BuildWebHost<Startup>(args);

        public static IWebHost BuildWebHost<TStartup>(string[] args) where TStartup : class =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((ctx, lb) =>
                {
                    TestSink sink = new TestSink();
                    lb.AddProvider(new TestLoggerProvider(sink));
                    lb.Services.Add(ServiceDescriptor.Singleton(sink));
                })
                .UseConfiguration(new ConfigurationBuilder()
                        .AddCommandLine(args)
                        .Build())
                .UseStartup<TStartup>()
                .UseStaticWebAssets()
                .Build();
    }
}
