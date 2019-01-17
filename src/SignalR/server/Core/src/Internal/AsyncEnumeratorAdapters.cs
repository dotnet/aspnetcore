// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    // True-internal because this is a weird and tricky class to use :)
    internal static class AsyncEnumeratorAdapters
    {
        public static IAsyncEnumerator<object> GetAsyncEnumeratorFromAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);

            if (typeof(T).IsValueType)
            {
                return new BoxedAsyncEnumerator<T>(enumerator);
            }

            return (IAsyncEnumerator<object>)enumerator;
        }

        public static IAsyncEnumerator<object> GetAsyncEnumeratorFromChannel<T>(ChannelReader<T> channel, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new ChannelAsyncEnumerator<T>(channel, cancellationToken);
        }

        /// <summary>Converts an IAsyncEnumerator of T to an IAsyncEnumerator of object.</summary>
        private class BoxedAsyncEnumerator<T> : IAsyncEnumerator<object>
        {
            private IAsyncEnumerator<T> _asyncEnumerator;

            public BoxedAsyncEnumerator(IAsyncEnumerator<T> asyncEnumerator)
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

        /// <summary>Provides an async enumerator for the data in a channel.</summary>
        private class ChannelAsyncEnumerator<T> : IAsyncEnumerator<object>
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
