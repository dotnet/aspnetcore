using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if WebJobs
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Azure.WebJobs.Host;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Company.Application1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
#if WebJobs
                .ConfigureWebJobs(webJobsBuilder => webJobsBuilder.AddTimers())
#endif
                .ConfigureServices(services => {
#if WebJobs
                    services.AddSingleton<ScheduleMonitor, FileSystemScheduleMonitor>();
#endif
#if Empty
                    services.AddHostedService<Worker>();
#endif
                });
    }
}
