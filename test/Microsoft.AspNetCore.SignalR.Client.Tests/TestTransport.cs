using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class TestTransport : ITransport
    {
        private readonly Func<Task> _stopHandler;
        private readonly Func<Task> _startHandler;

        public TransferMode? Mode { get; }
        public Channel<byte[], SendMessage> Application { get; private set; }

        public TestTransport(Func<Task> onTransportStop = null, Func<Task> onTransportStart = null, TransferMode transferMode = TransferMode.Text)
        {
            _stopHandler = onTransportStop ?? new Func<Task>(() => Task.CompletedTask);
            _startHandler = onTransportStart ?? new Func<Task>(() => Task.CompletedTask);
            Mode = transferMode;
        }

        public Task StartAsync(Uri url, Channel<byte[], SendMessage> application, TransferMode requestedTransferMode, IConnection connection)
        {
            Application = application;
            return _startHandler();
        }

        public async Task StopAsync()
        {
            await _stopHandler();
            Application.Writer.TryComplete();
        }
    }
}
