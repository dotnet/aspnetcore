using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class TestTransport : ITransport
    {
        private readonly Func<Task> _stopHandler;
        private readonly Func<Task> _startHandler;

        public TransferFormat? Format { get; }
        public IDuplexPipe Application { get; private set; }
        public Task Receiving { get; private set; }

        public TestTransport(Func<Task> onTransportStop = null, Func<Task> onTransportStart = null, TransferFormat transferFormat = TransferFormat.Text)
        {
            _stopHandler = onTransportStop ?? new Func<Task>(() => Task.CompletedTask);
            _startHandler = onTransportStart ?? new Func<Task>(() => Task.CompletedTask);
            Format = transferFormat;
        }

        public async Task StartAsync(Uri url, IDuplexPipe application, TransferFormat transferFormat, IConnection connection)
        {
            if ((Format & transferFormat) == 0)
            {
                throw new InvalidOperationException($"The '{transferFormat}' transfer format is not supported by this transport.");
            }
            Application = application;
            await _startHandler();

            // Start a loop to read from the pipe
            Receiving = ReceiveLoop();
            async Task ReceiveLoop()
            {
                while (true)
                {
                    var result = await Application.Input.ReadAsync();
                    if (result.IsCompleted)
                    {
                        break;
                    }
                    else if (result.IsCanceled)
                    {
                        // This is useful for detecting that the connection tried to gracefully terminate.
                        // If the Receiving task is faulted/cancelled, it means StopAsync was the thing that
                        // actually terminated the connection (not ideal, we want the transport pipe to
                        // shut down gracefully)
                        throw new OperationCanceledException();
                    }

                    Application.Input.AdvanceTo(result.Buffer.End);
                }

                // Call the transport stop handler
                await _stopHandler();

                // Complete our end of the pipe
                Application.Output.Complete();
                Application.Input.Complete();
            }
        }

        public Task StopAsync()
        {
            Application.Input.CancelPendingRead();
            return Receiving;
        }
    }
}
