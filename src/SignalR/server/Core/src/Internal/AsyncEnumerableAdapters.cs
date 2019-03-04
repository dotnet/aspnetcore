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

        public static IAsyncEnumerable<object> MakeCancelableAsyncEnumerableFromChannel<T>(ChannelReader<T> channel, CancellationToken cancellationToken = default)
        {
            return MakeCancelableAsyncEnumerable(channel.ReadAllAsync(), cancellationToken);
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
                // Assume that this will be iterated through with await foreach which always passes a default token.
                // Instead use the token from the ctor.
                Debug.Assert(cancellationToken == default);

                var enumeratorOfT = _asyncEnumerable.GetAsyncEnumerator(_cancellationToken);
                return enumeratorOfT as IAsyncEnumerator<object> ?? new BoxedAsyncEnumerator(enumeratorOfT);
            }

            private class BoxedAsyncEnumerator : IAsyncEnumerator<object>
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
        }
    }
}
