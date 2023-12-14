// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal;

// True-internal because this is a weird and tricky class to use :)
internal static class AsyncEnumerableAdapters
{
    public static IAsyncEnumerator<object?> MakeCancelableAsyncEnumerator<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken = default)
    {
        var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
        return enumerator as IAsyncEnumerator<object?> ?? new BoxedAsyncEnumerator<T>(enumerator);
    }

    public static IAsyncEnumerable<T> MakeCancelableTypedAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationTokenSource cts)
    {
        return new CancelableTypedAsyncEnumerable<T>(asyncEnumerable, cts);
    }

    public static IAsyncEnumerator<object?> MakeAsyncEnumeratorFromChannel<T>(ChannelReader<T> channel, CancellationToken cancellationToken = default)
    {
        return new ChannelAsyncEnumerator<T>(channel, cancellationToken);
    }

    private sealed class ChannelAsyncEnumerator<T> : IAsyncEnumerator<object?>
    {
        private readonly ChannelReader<T> _channel;
        private readonly CancellationToken _cancellationToken;
        public ChannelAsyncEnumerator(ChannelReader<T> channel, CancellationToken cancellationToken)
        {
            _channel = channel;
            _cancellationToken = cancellationToken;
        }

        public object? Current { get; private set; }

        public ValueTask<bool> MoveNextAsync()
        {
            if (_channel.TryRead(out var item))
            {
                Current = item;
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(MoveNextAsyncAwaited());
        }

        private async Task<bool> MoveNextAsyncAwaited()
        {
            if (await _channel.WaitToReadAsync(_cancellationToken).ConfigureAwait(false) && _channel.TryRead(out var item))
            {
                Current = item;
                return true;
            }
            return false;
        }

        public ValueTask DisposeAsync() => default;
    }

    private sealed class CancelableTypedAsyncEnumerable<TResult> : IAsyncEnumerable<TResult>
    {
        private readonly IAsyncEnumerable<TResult> _asyncEnumerable;
        private readonly CancellationTokenSource _cts;

        public CancelableTypedAsyncEnumerable(IAsyncEnumerable<TResult> asyncEnumerable, CancellationTokenSource cts)
        {
            _asyncEnumerable = asyncEnumerable;
            _cts = cts;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var enumerator = _asyncEnumerable.GetAsyncEnumerator(_cts.Token);
            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register((ctsState) =>
                {
                    ((CancellationTokenSource)ctsState!).Cancel();
                }, _cts);

                return new CancelableEnumerator<TResult>(enumerator, registration);
            }

            return enumerator;
        }

        private sealed class CancelableEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _asyncEnumerator;
            private readonly CancellationTokenRegistration _cancellationTokenRegistration;

            public T Current => (T)_asyncEnumerator.Current;

            public CancelableEnumerator(IAsyncEnumerator<T> asyncEnumerator, CancellationTokenRegistration registration)
            {
                _asyncEnumerator = asyncEnumerator;
                _cancellationTokenRegistration = registration;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return _asyncEnumerator.MoveNextAsync();
            }

            public ValueTask DisposeAsync()
            {
                _cancellationTokenRegistration.Dispose();
                return _asyncEnumerator.DisposeAsync();
            }
        }
    }

    private sealed class BoxedAsyncEnumerator<T> : IAsyncEnumerator<object?>
    {
        private readonly IAsyncEnumerator<T> _asyncEnumerator;

        public BoxedAsyncEnumerator(IAsyncEnumerator<T> asyncEnumerator)
        {
            _asyncEnumerator = asyncEnumerator;
        }

        public object? Current => _asyncEnumerator.Current;

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
