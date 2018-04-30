using System;
using System.IO;
using System.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SampleMsmqHost
{
    public interface IMsmqConnection
    {
        void SendText(string text);

        Task<Message> ReceiveAsync(CancellationToken cancellationToken);
    }

    public class MsmqConnection : IMsmqConnection, IDisposable
    {
        private readonly MessageQueue _queue;

        public MsmqOptions Options { get; }

        public ILogger<MsmqConnection> Logger { get; }

        public MsmqConnection(IOptions<MsmqOptions> options, ILogger<MsmqConnection> logger)
        {
            Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _queue = OpenQueue();
        }

        private MessageQueue OpenQueue()
        {
            Logger.LogInformation("Opening Queue: Path={0}; AccessMode={1};", Options.Path, Options.AccessMode);

            return new MessageQueue(Options.Path, Options.SharedModeDenyReceive, Options.EnableCache, Options.AccessMode);
        }

        public void Dispose()
        {
            Logger.LogInformation("Closing Queue");

            _queue?.Dispose();
        }

        public void SendText(string text)
        {
            // send the text message as UTF7
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF7))
            using (var message = new Message())
            {
                writer.Write(text);
                writer.Flush();

                message.BodyStream = stream;

                _queue.Send(message);
            }
        }

        public async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<Message>();
            using (cancellationToken.Register(obj => ((TaskCompletionSource<Message>)obj).TrySetCanceled(), tcs))
            {
                // wait for a message to arrive or cancellation
                var receiveTask = Task.Factory.FromAsync(_queue.BeginReceive(), _queue.EndReceive);
                if (receiveTask != await Task.WhenAny(receiveTask, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);

                return receiveTask.Result;
            }
        }

    }
}