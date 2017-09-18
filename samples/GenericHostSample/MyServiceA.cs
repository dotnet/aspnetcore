using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class MyServiceA : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("MyServiceA is starting.");

            stoppingToken.Register(() => Console.WriteLine("MyServiceA is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("MyServiceA is doing background work.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            Console.WriteLine("MyServiceA background task is stopping.");
        }
    }
}
