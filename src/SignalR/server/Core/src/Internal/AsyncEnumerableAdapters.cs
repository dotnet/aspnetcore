// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    // True-internal because this is a weird and tricky class to use :)
    internal static class AsyncEnumerableAdapters
    {
        public static IAsyncEnumerable<object> MakeCancelableAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken = default)
        {
            return new CancelableAsyncEnumerable<T>(asyncEnumerable, cancellationToken);
        }

        public static IAsyncEnumerable<object> GetAsyncEnumerableFromChannel<T>(ChannelReader<T> channel, CancellationToken cancellationToken = default)
        {
            return new ChannelAsyncEnumerable<T>(channel, cancellationToken);
        }

        /// <summary>Converts an IAsyncEnumerable of T to an IAsyncEnumerable of object.</summary>
        private class CancelableAsyncEnumerable<T> : IAsyncEnumerable<object>
        {
            private readonly IAsyncEnumerable<T> _asyncEnumerable;
            private readonly CancellationToken _cancellationToken;

            public CancelableAsyncEnumerable(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken)
            {
                _asyncEnumerable = asyncEnumerable;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                Debug.Assert(cancellationToken == default);
                return new CancelableAsyncEnumerator(_asyncEnumerable.GetAsyncEnumerator(_cancellationToken));
            }

            private class CancelableAsyncEnumerator : IAsyncEnumerator<object>
            {
                private IAsyncEnumerator<T> _asyncEnumerator;

                public CancelableAsyncEnumerator(IAsyncEnumerator<T> asyncEnumerator)
                {
                    _asyncEnumerator = asyncEnumerator;
                }

                public object Current => _asyncEnumerator.Current;

                public ValueTask<bool> MoveNextAsync()
                {
                    return _asyncEnumerator.MoveNextAsync();
                }

                public ValueTask DisposeAsync()
                {
                    return _asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>Provides an IAsyncEnumerable of object for the data in a channel.</summary>
        private class ChannelAsyncEnumerable<T> : IAsyncEnumerable<object>
        {
            private readonly ChannelReader<T> _channel;
            private readonly CancellationToken _cancellationToken;

            public ChannelAsyncEnumerable(ChannelReader<T> channel, CancellationToken cancellationToken)
            {
                _channel = channel;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                Debug.Assert(cancellationToken == default);
                return new ChannelAsyncEnumerator(_channel, _cancellationToken);
            }

            private class ChannelAsyncEnumerator : IAsyncEnumerator<object>
            {
                /// <summary>The channel being enumerated.</summary>
                private readonly ChannelReader<T> _channel;
                /// <summary>Cancellation token used to cancel the enumeration.</summary>
                private readonly CancellationToken _cancellationToken;
                /// <summary>The current element of the enumeration.</summary>
                private T _current;

                public ChannelAsyncEnumerator(ChannelReader<T> channel, CancellationToken cancellationToken)
                {
                    _channel = channel;
                    _cancellationToken = cancellationToken;
                }

                public object Current => _current;

                public ValueTask<bool> MoveNextAsync()
                {
                    var result = _channel.ReadAsync(_cancellationToken);

                    if (result.IsCompletedSuccessfully)
                    {
                        _current = result.Result;
                        return new ValueTask<bool>(true);
                    }

                    return new ValueTask<bool>(MoveNextAsyncAwaited(result));
                }

                private async Task<bool> MoveNextAsyncAwaited(ValueTask<T> channelReadTask)
                {
                    try
                    {
                        _current = await channelReadTask;
                    }
                    catch (ChannelClosedException ex) when (ex.InnerException == null)
                    {
                        return false;
                    }

                    return true;
                }

                public ValueTask DisposeAsync()
                {
                    return default;
                }
            }
        }
    }
}
