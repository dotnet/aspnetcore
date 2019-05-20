using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Company.WebApplication1
{
    public class StreamingTimerHub : Hub
    {
        public ChannelReader<string> ServerTimer(CancellationToken token)
        {
            var channel = Channel.CreateUnbounded<string>();
            _ = WriteDateAsync(channel.Writer, token);
            return channel.Reader;
        }

        private async Task WriteDateAsync(ChannelWriter<string> writer, 
            CancellationToken token)
        {
            try
            {
                while(true)
                {
                    token.ThrowIfCancellationRequested();
                    await writer.WriteAsync(DateTime.Now.ToString("HH:mm:ss.fffffff"));
                    await Task.Delay(10, token);
                }
            }
            catch
            {
                writer.TryComplete();
            }
            
            writer.TryComplete();
        }
    }
}