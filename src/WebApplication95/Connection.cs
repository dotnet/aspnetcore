using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Channels;

namespace WebApplication95
{
    public enum TransportType
    {
        LongPolling,
        WebSockets,
        ServerSentEvents
    }

    public class Connection : IChannel
    {
        public TransportType TransportType { get; set; }

        public string ConnectionId { get; set; }

        IReadableChannel IChannel.Input => Input;

        IWritableChannel IChannel.Output => Output;

        internal Channel Input { get; set; }

        internal Channel Output { get; set; }

        public Connection()
        {
            Stream = new ChannelStream(this);
        }

        public Stream Stream { get; }

        public void Complete()
        {
            Input.CompleteReader();
            Input.CompleteWriter();

            Output.CompleteReader();
            Output.CompleteWriter();
        }

        public void Dispose()
        {

        }
    }
}
