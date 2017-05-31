using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR;

namespace SocketsSample.Hubs
{
    public class Streaming : Hub
    {
        public IObservable<int> ObservableCounter(int count, int delay)
        {
            return new CounterObservable(count, delay);
        }

        public ReadableChannel<int> ChannelCounter(int count, int delay)
        {
            var channel = Channel.CreateUnbounded<int>();

            Task.Run(async () =>
            {
                for (var i = 0; i < count; i++)
                {
                    await channel.Out.WriteAsync(i);
                    await Task.Delay(delay);
                }

                channel.Out.TryComplete();
            });

            return channel.In;
        }

        private class CounterObservable : IObservable<int>
        {
            private int _count;
            private int _delay;

            public CounterObservable(int count, int delay)
            {
                _count = count;
                _delay = delay;
            }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                // Run in a thread-pool thread
                var cts = new CancellationTokenSource();
                Task.Run(async () =>
                {
                    for (var i = 0; !cts.Token.IsCancellationRequested && i < _count; i++)
                    {
                        observer.OnNext(i);
                        await Task.Delay(_delay);
                    }
                    observer.OnCompleted();
                });

                return new Disposable(() => cts.Cancel());
            }
        }

        private class Disposable : IDisposable
        {
            private Action _action;

            public Disposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}
