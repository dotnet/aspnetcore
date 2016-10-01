using System;
using Channels;

namespace WebApplication95
{
    public class HttpChannel : IChannel
    {
        public HttpChannel(ChannelFactory factory)
        {
            Input = factory.CreateChannel();
            Output = factory.CreateChannel();
        }

        IReadableChannel IChannel.Input => Input;

        IWritableChannel IChannel.Output => Output;

        public Channel Input { get; }

        public Channel Output { get; }

        public void Dispose()
        {
            Input.CompleteReader();
            Input.CompleteWriter();

            Output.CompleteReader();
            Output.CompleteWriter();
        }
    }
}
