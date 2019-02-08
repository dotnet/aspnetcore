using System;
using System.Threading;
using System.Threading.Tasks;
#if WebJobs
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
#endif
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Company.Application1
{
#if WebJobs
    public class Worker
#elif Empty
    public class Worker : BackgroundService
#endif
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

#if Empty
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker running at: {DateTime.Now}");
                await Task.Delay(1000);
            }
        }
#elif WebJobs
        public void LogMessage([TimerTrigger("* * * * *")]TimerInfo timer)
        {
            _logger.LogInformation($"Worker running at: {DateTime.Now}");
        }
#endif
    }
}
