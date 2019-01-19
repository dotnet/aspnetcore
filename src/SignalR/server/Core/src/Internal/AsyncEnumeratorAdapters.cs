// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    // True-internal because this is a weird and tricky class to use :)
    internal static class AsyncEnumeratorAdapters
    {
        public static IAsyncEnumerator<object> GetAsyncEnumerator<T>(ChannelReader<T> channel, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Nothing to dispose when we finish enumerating in this case.
            return new AsyncEnumerator<T>(channel, cancellationToken, disposable: null);
        }

        /// <summary>Provides an async enumerator for the data in a channel.</summary>
        internal class AsyncEnumerator<T> : IAsyncEnumerator<object>, IDisposable
        {
            /// <summary>The channel being enumerated.</summary>
            private readonly ChannelReader<T> _channel;
            /// <summary>Cancellation token used to cancel the enumeration.</summary>
            private readonly CancellationToken _cancellationToken;
            /// <summary>The current element of the enumeration.</summary>
            private object _current;

            private readonly IDisposable _disposable;

            internal AsyncEnumerator(ChannelReader<T> channel, CancellationToken cancellationToken, IDisposable disposable)
            {
                _channel = channel;
                _cancellationToken = cancellationToken;
                _disposable = disposable;
            }

            public object Current => _current;

            public Task<bool> MoveNextAsync()
            {
                var result = _channel.ReadAsync(_cancellationToken);

                if (result.IsCompletedSuccessfully)
                {
                    _current = result.Result;
                    return Task.FromResult(true);
                }

                return result.AsTask().ContinueWith((t, s) =>
                {
                    var thisRef = (AsyncEnumerator<T>)s;
                    if (t.IsFaulted && t.Exception.InnerException is ChannelClosedException cce && cce.InnerException == null)
                    {
                        return false;
                    }
                    thisRef._current = t.GetAwaiter().GetResult();
                    return true;
                }, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
            }

            public void Dispose()
            {
                _disposable?.Dispose();
            }
        }
    }

    /// <summary>Represents an enumerator accessed asynchronously.</summary>
    /// <typeparam name="T">Specifies the type of the data enumerated.</typeparam>
    internal interface IAsyncEnumerator<out T>
    {
        /// <summary>Asynchronously move the enumerator to the next element.</summary>
        /// <returns>
        /// A task that returns true if the enumerator was successfully advanced to the next item,
        /// or false if no more data was available in the collection.
        /// </returns>
        Task<bool> MoveNextAsync();

        /// <summary>Gets the current element being enumerated.</summary>
        T Current { get; }
    }
}
