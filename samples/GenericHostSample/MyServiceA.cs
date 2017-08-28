using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class MyServiceA : IHostedService
    {
        private bool _stopping;
        private Task _backgroundTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceA is starting.");
            _backgroundTask = BackgroundTask();
            return Task.CompletedTask;
        }

        private async Task BackgroundTask()
        {
            while (!_stopping)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                Console.WriteLine("MyServiceA is doing background work.");
            }

            Console.WriteLine("MyServiceA background task is stopping.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceA is stopping.");
            _stopping = true;
            if (_backgroundTask != null)
            {
                // TODO: cancellation
                await _backgroundTask;
            }
        }
    }
}
