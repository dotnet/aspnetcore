#if NET461
using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public static class ServiceBaseLifetimeHostExtensions
    {
        public static IHostBuilder UseServiceBaseLifetime(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((hostContext, services) => services.AddSingleton<IHostLifetime, ServiceBaseLifetime>());
        }

        public static Task RunAsServiceAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default)
        {
            return hostBuilder.UseServiceBaseLifetime().Build().RunAsync(cancellationToken);
        }
    }

    public class ServiceBaseLifetime : ServiceBase, IHostLifetime
    {
        private Action<object> _startCallback;
        private Action<object> _stopCallback;
        private object _startState;
        private object _stopState;

        public void RegisterDelayStartCallback(Action<object> callback, object state)
        {
            _startCallback = callback ?? throw new ArgumentNullException(nameof(callback));
            _startState = state;

            Run(this);
        }

        public void RegisterStopCallback(Action<object> callback, object state)
        {
            _stopCallback = callback ?? throw new ArgumentNullException(nameof(callback));
            _stopState = state;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stop();
            return Task.CompletedTask;
        }

        protected override void OnStart(string[] args)
        {
            _startCallback(_startState);
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            _stopCallback(_stopState);
            base.OnStop();
        }
    }
}
#endif