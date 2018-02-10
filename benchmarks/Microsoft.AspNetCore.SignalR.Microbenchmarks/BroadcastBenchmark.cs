using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class BroadcastBenchmark
    {
        private DefaultHubLifetimeManager<Hub> _hubLifetimeManager;
        private HubContext<Hub> _hubContext;

        [Params(1, 10, 1000)]
        public int Connections;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _hubLifetimeManager = new DefaultHubLifetimeManager<Hub>();
            var options = new UnboundedChannelOptions { AllowSynchronousContinuations = true };

            for (var i = 0; i < Connections; ++i)
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Application, pair.Transport);

                _hubLifetimeManager.OnConnectedAsync(new HubConnectionContext(connection, Timeout.InfiniteTimeSpan, NullLoggerFactory.Instance)).Wait();
            }

            _hubContext = new HubContext<Hub>(_hubLifetimeManager);
        }

        [Benchmark]
        public Task SendAsyncAll()
        {
            return _hubContext.Clients.All.SendAsync("Method");
        }
    }
}
