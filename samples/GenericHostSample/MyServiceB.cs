using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class MyServiceB : IHostedService, IDisposable
    {
        private bool _stopping;
        private Task _backgroundTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceB is starting.");
            _backgroundTask = BackgroundTask();
            return Task.CompletedTask;
        }

        private async Task BackgroundTask()
        {
            while (!_stopping)
            {
                await Task.Delay(TimeSpan.FromSeconds(7));
                Console.WriteLine("MyServiceB is doing background work.");
            }

            Console.WriteLine("MyServiceB background task is stopping.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceB is stopping.");
            _stopping = true;
            if (_backgroundTask != null)
            {
                // TODO: cancellation
                await _backgroundTask;
            }
        }

        public void Dispose()
        {
            Console.WriteLine("MyServiceB is disposing.");
        }
    }
}
