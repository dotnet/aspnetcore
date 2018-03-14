using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class TestTransport : ITransport
    {
        private readonly Func<Task> _stopHandler;
        private readonly Func<Task> _startHandler;

        public TransferFormat? Format { get; }
        public IDuplexPipe Application { get; private set; }

        public TestTransport(Func<Task> onTransportStop = null, Func<Task> onTransportStart = null, TransferFormat transferFormat = TransferFormat.Text)
        {
            _stopHandler = onTransportStop ?? new Func<Task>(() => Task.CompletedTask);
            _startHandler = onTransportStart ?? new Func<Task>(() => Task.CompletedTask);
            Format = transferFormat;
        }

        public Task StartAsync(Uri url, IDuplexPipe application, TransferFormat transferFormat, IConnection connection)
        {
            if ((Format & transferFormat) == 0)
            {
                throw new InvalidOperationException($"The '{transferFormat}' transfer format is not supported by this transport.");
            }
            Application = application;
            return _startHandler();
        }

        public async Task StopAsync()
        {
            await _stopHandler();
            Application.Output.Complete();
        }
    }
}
