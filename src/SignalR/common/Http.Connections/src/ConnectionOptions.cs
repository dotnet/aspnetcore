// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Connections;

/// <summary>
/// Options used to change behavior of how connections are handled.
/// </summary>
public class ConnectionOptions
{
    /// <summary>
    /// Gets or sets the interval used by the server to timeout idle connections.
    /// </summary>
    public TimeSpan? DisconnectTimeout { get; set; }

    internal List<Func<Task>> ShutdownCallbacks = new();

    public IDisposable RegisterBeforeShutdown(Func<Task> callback)
    {
        ShutdownCallbacks.Add(callback);
        return new CallbackDisposable(ShutdownCallbacks, callback);
    }

    private class CallbackDisposable : IDisposable
    {
        private readonly List<Func<Task>> _list;
        private readonly Func<Task> _func;

        public CallbackDisposable(List<Func<Task>> list, Func<Task> func)
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
