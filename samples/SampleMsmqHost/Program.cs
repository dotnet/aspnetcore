using System;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleMsmqHost
{
    public static class Program
    {
        // Before running this program, please make sure to install MSMQ
        // and create the ".\private$\SampleQueue" queue on your local machine.

        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddCommandLine(args);
                })
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                })
                .ConfigureServices(services =>
                {
                    services.AddOptions();

                    services.Configure<MsmqOptions>(options =>
                    {
                        options.Path = @".\private$\SampleQueue";
                        options.AccessMode = QueueAccessMode.SendAndReceive;
                    });

                    services.AddSingleton<IMsmqConnection, MsmqConnection>();
                    services.AddTransient<IMsmqProcessor, MsmqProcessor>();
                    services.AddTransient<IHostedService, MsmqService>();
                })
                .Build();

            using (host)
            {
                // start the MSMQ host
                await host.StartAsync();

                // read and dispatch messages to the MSMQ queue
                StartReadLoop(host);

                // wait for the MSMQ host to shutdown
                await host.WaitForShutdownAsync();
            }
        }

        private static void StartReadLoop(IHost host)
        {
            var connection = host.Services.GetRequiredService<IMsmqConnection>();
            var applicationLifetime = host.Services.GetRequiredService<IApplicationLifetime>();

            // run the read loop in a background thread so that it can be stopped with CTRL+C
            Task.Run(() => ReadLoop(connection, applicationLifetime.ApplicationStopping));
        }

        private static void ReadLoop(IMsmqConnection connection, CancellationToken cancellationToken)
        {
            Console.WriteLine("Enter your text message and press ENTER...");

            while (!cancellationToken.IsCancellationRequested)
            {
                // read a text message from the user
                cancellationToken.ThrowIfCancellationRequested();
                var text = Console.ReadLine();

                // send the text message to the queue
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(text))
                    connection.SendText(text);
            }
        }

    }
}