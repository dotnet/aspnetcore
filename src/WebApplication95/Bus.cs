using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication95
{
    public class Message
    {
        public string ContentType { get; set; }
        public ArraySegment<byte> Payload { get; set; }
    }

    public class Bus
    {
        private readonly ConcurrentDictionary<string, List<IObserver<Message>>> _subscriptions = new ConcurrentDictionary<string, List<IObserver<Message>>>();

        public IDisposable Subscribe(string key, IObserver<Message> observer)
        {
            var connections = _subscriptions.GetOrAdd(key, _ => new List<IObserver<Message>>());
            connections.Add(observer);

            return new DisposableAction(() =>
            {
                connections.Remove(observer);
            });
        }

        public void Publish(string key, Message message)
        {
            List<IObserver<Message>> connections;
            if (_subscriptions.TryGetValue(key, out connections))
            {
                foreach (var c in connections)
                {
                    c.OnNext(message);
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
