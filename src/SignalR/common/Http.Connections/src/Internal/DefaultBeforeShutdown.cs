// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    internal class DefaultBeforeShutdown : IBeforeShutdown
    {
        internal List<Func<Task>> Callbacks { get; } = new();

        public IDisposable Register(Func<Task> func)
        {
            lock (Callbacks)
            {
                Callbacks.Add(func);
            }
            return new Disposable(Callbacks, func);
        }

        public async Task TriggerAsync()
        {
            Func<Task>[] callbacks;
            lock (Callbacks)
            {
                callbacks = Callbacks.ToArray();
                Callbacks.Clear();
            }
            foreach (var callback in callbacks)
            {
                try
                {
                    await callback();
                }
                catch { }
            }
        }

        private class Disposable : IDisposable
        {
            private readonly List<Func<Task>> _list;
            private readonly Func<Task> _func;

            public Disposable(List<Func<Task>> list, Func<Task> func)
            {
                _list = list;
                _func = func;
            }

            public void Dispose()
            {
                lock (_list)
                {
                    _list.Remove(_func);
                }
            }
        }
    }
}
