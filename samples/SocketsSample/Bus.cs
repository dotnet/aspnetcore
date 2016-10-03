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
        public byte[] Payload { get; set; }
    }

    public class Bus
    {
        private readonly ConcurrentDictionary<string, List<Func<Message, Task>>> _subscriptions = new ConcurrentDictionary<string, List<Func<Message, Task>>>();

        public IDisposable Subscribe(string key, Func<Message, Task> observer)
        {
            var connections = _subscriptions.GetOrAdd(key, _ => new List<Func<Message, Task>>());
            lock (connections)
            {
                connections.Add(observer);
            }

            return new DisposableAction(() =>
            {
                lock (connections)
                {
                    connections.Remove(observer);
                }
            });
        }

        public async Task Publish(string key, Message message)
        {
            List<Func<Message, Task>> connections;
            if (_subscriptions.TryGetValue(key, out connections))
            {
                Task[] tasks = null;
                lock (connections)
                {
                    tasks = new Task[connections.Count];
                    for (int i = 0; i < connections.Count; i++)
                    {
                        tasks[i] = connections[i](message);
                    }
                }

                await Task.WhenAll(tasks);
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
