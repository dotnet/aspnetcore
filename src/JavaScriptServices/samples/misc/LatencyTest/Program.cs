using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.NodeServices.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApplication
{
    // This project is a micro-benchmark for .NET->Node RPC via NodeServices. It doesn't reflect
    // real-world usage patterns (you're not likely to make hundreds of sequential calls like this),
    // but is a starting point for comparing the overhead of different hosting models and transports.
    public class Program
    {
        public static void Main(string[] args) {
            // Set up the DI system
            var services = new ServiceCollection();
            services.AddNodeServices(options => {
                // To compare with Socket hosting, uncomment the following line
                // Since .NET Core 1.1, the HTTP hosting model has become basically as fast as the Socket hosting model
                //options.UseSocketHosting();

                options.WatchFileExtensions = new string[] {}; // Don't watch anything
            });
            var serviceProvider = services.BuildServiceProvider();

            // Now instantiate an INodeServices and use it
            using (var nodeServices = serviceProvider.GetRequiredService<INodeServices>()) {
                MeasureLatency(nodeServices).Wait();
            }
        }

        private static async Task MeasureLatency(INodeServices nodeServices) {
            // Ensure the connection is open, so we can measure per-request timings below
            var response = await nodeServices.InvokeAsync<string>("latencyTest", "C#");
            Console.WriteLine(response);

            // Now perform a series of requests, capturing the time taken
            const int requestCount = 100;
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < requestCount; i++) {
                await nodeServices.InvokeAsync<string>("latencyTest", "C#");
            }

            // Display results
            var elapsedSeconds = (float)watch.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine("\nTotal time: {0:F2} milliseconds", 1000 * elapsedSeconds);
            Console.WriteLine("\nTime per invocation: {0:F2} milliseconds", 1000 * elapsedSeconds / requestCount);
        }
    }
}
