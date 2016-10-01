using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Channels;

namespace Microsoft.AspNetCore.Sockets
{
    public class Message
    {
        public ReadableBuffer Payload { get; set; }
    }

    public class Bus
    {
        private readonly ConcurrentDictionary<string, List<Func<Message, Task>>> _subscriptions = new ConcurrentDictionary<string, List<Func<Message, Task>>>();

        public IDisposable Subscribe(string key, Func<Message, Task> observer)
        {
            var connections = _subscriptions.GetOrAdd(key, _ => new List<Func<Message, Task>>());
            connections.Add(observer);

            return new DisposableAction(() =>
            {
                connections.Remove(observer);
            });
        }

        public async Task Publish(string key, Message message)
        {
            List<Func<Message, Task>> connections;
            if (_subscriptions.TryGetValue(key, out connections))
            {
                foreach (var c in connections)
                {
                    await c(message);
                }
            }
        }

        private class DisposableAction : IDisposable
        {
            private Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _action, () => { }).Invoke();
            }
        }
    }
}
