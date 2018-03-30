using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared
{
    public class TestConnection : IConnection
    {
        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(TransferFormat transferFormat)
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public IDuplexPipe Transport { get; set; }

        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
