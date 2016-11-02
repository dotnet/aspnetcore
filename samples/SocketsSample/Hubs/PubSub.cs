using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SocketsSample.Hubs
{
    public interface IPubSub
    {
        IDisposable Subscribe(string topic, Func<object, Task> callback);
        Task Publish(string topic, object data);
    }

    public class Bus : IPubSub
    {
        private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _subscriptions = new ConcurrentDictionary<string, List<Func<object, Task>>>();

        public IDisposable Subscribe(string key, Func<object, Task> observer)
        {
            var subscriptions = _subscriptions.GetOrAdd(key, _ => new List<Func<object, Task>>());
            subscriptions.Add(observer);

            return new DisposableAction(() =>
            {
                subscriptions.Remove(observer);
            });
        }

        public async Task Publish(string key, object data)
        {
            List<Func<object, Task>> subscriptions;
            if (_subscriptions.TryGetValue(key, out subscriptions))
            {
                foreach (var c in subscriptions)
                {
                    await c(data);
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
