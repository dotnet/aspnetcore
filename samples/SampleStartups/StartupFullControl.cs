using System;
using System.IO;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups
{
    public class StartupFullControl
    {
        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseServer("Microsoft.AspNet.Server.Kestrel") // Set the server manually
                .UseApplicationBasePath(Directory.GetCurrentDirectory()) // Override the application base with the current directory
                .UseUrls("http://*:1000", "https://*:902")
                .UseEnvironment("Development")
                .UseWebRoot("public")
                .ConfigureServices(services =>
                {
                    // Configure services that the application can see
                    services.AddSingleton<IMyCustomService, MyCustomService>();
                })
                .Configure(app =>
                {
                    // Write the application inline, this won't call any startup class in the assembly

                    app.Use(next => context =>
                    {
                        return next(context);
                    });
                })
                .Build();

            application.Run();
        }
    }

    public class MyHostLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public interface IMyCustomService
    {
        void Go();
    }

    public class MyCustomService : IMyCustomService
    {
        public void Go()
        {
            throw new NotImplementedException();
        }
    }
}
