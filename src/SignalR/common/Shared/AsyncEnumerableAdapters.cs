// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal;

// True-internal because this is a weird and tricky class to use :)
internal static class AsyncEnumerableAdapters
{
    public static IAsyncEnumerator<object?> MakeAsyncEnumerator<T>(IAsyncEnumerable<T> asyncEnumerable, CancellationToken cancellationToken = default)
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

            return MoveNextAsyncAwaited();
        }

        private async ValueTask<bool> MoveNextAsyncAwaited()
        {
            while (await _channel.WaitToReadAsync(_cancellationToken).ConfigureAwait(false))
            {
                if (_channel.TryRead(out var item))
                {
                    Current = item;
                    return true;
                }
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

#if NET6_0_OR_GREATER

    private static readonly MethodInfo _asyncEnumerableGetAsyncEnumeratorMethodInfo = typeof(IAsyncEnumerable<>).GetMethod("GetAsyncEnumerator")!;

    /// <summary>
    /// Creates an IAsyncEnumerator{object} from an IAsyncEnumerable{T} using reflection.
    ///
    /// Used when the runtime does not support dynamic code generation (ex. native AOT) and the generic type is a value type. In this scenario,
    /// we cannot use MakeGenericMethod to call a generic method because the generic type is a value type.
    /// </summary>
    public static IAsyncEnumerator<object?> MakeReflectionAsyncEnumerator(object asyncEnumerable, CancellationToken cancellationToken)
    {
        var constructedIAsyncEnumerableInterface = ReflectionHelper.GetIAsyncEnumerableInterface(asyncEnumerable.GetType())!;
        var enumerator = ((MethodInfo)constructedIAsyncEnumerableInterface.GetMemberWithSameMetadataDefinitionAs(_asyncEnumerableGetAsyncEnumeratorMethodInfo)).Invoke(asyncEnumerable, [cancellationToken])!;
        return new ReflectionAsyncEnumerator(enumerator);
    }

    /// <summary>
    /// Creates an IAsyncEnumerator{object} from a ChannelReader{T} using reflection.
    ///
    /// Used when the runtime does not support dynamic code generation (ex. native AOT) and the generic type is a value type. In this scenario,
    /// we cannot use MakeGenericMethod to call a generic method because the generic type is a value type.
    /// </summary>
    public static IAsyncEnumerator<object?> MakeReflectionAsyncEnumeratorFromChannel(object channelReader, CancellationToken cancellationToken)
    {
        return new ReflectionChannelAsyncEnumerator(channelReader, cancellationToken);
    }

    private sealed class ReflectionAsyncEnumerator : IAsyncEnumerator<object?>
    {
        private static readonly MethodInfo _asyncEnumeratorMoveNextAsyncMethodInfo = typeof(IAsyncEnumerator<>).GetMethod("MoveNextAsync")!;
        private static readonly MethodInfo _asyncEnumeratorGetCurrentMethodInfo = typeof(IAsyncEnumerator<>).GetMethod("get_Current")!;

        private readonly object _enumerator;
        private readonly MethodInfo _moveNextAsyncMethodInfo;
        private readonly MethodInfo _getCurrentMethodInfo;

        public ReflectionAsyncEnumerator(object enumerator)
        {
            _enumerator = enumerator;

            var type = ReflectionHelper.GetIAsyncEnumeratorInterface(enumerator.GetType());
            _moveNextAsyncMethodInfo = (MethodInfo)type.GetMemberWithSameMetadataDefinitionAs(_asyncEnumeratorMoveNextAsyncMethodInfo)!;
            _getCurrentMethodInfo = (MethodInfo)type.GetMemberWithSameMetadataDefinitionAs(_asyncEnumeratorGetCurrentMethodInfo)!;
        }

        public object? Current => _getCurrentMethodInfo.Invoke(_enumerator, []);

        public ValueTask<bool> MoveNextAsync() => (ValueTask<bool>)_moveNextAsyncMethodInfo.Invoke(_enumerator, [])!;

        public ValueTask DisposeAsync() => ((IAsyncDisposable)_enumerator).DisposeAsync();
    }

    private sealed class ReflectionChannelAsyncEnumerator : IAsyncEnumerator<object?>
    {
        private static readonly MethodInfo _channelReaderTryReadMethodInfo = typeof(ChannelReader<>).GetMethod("TryRead")!;
        private static readonly MethodInfo _channelReaderWaitToReadAsyncMethodInfo = typeof(ChannelReader<>).GetMethod("WaitToReadAsync")!;

        private readonly object _channelReader;
        private readonly object?[] _tryReadResult = [null];
        private readonly object[] _waitToReadArgs;
        private readonly MethodInfo _tryReadMethodInfo;
        private readonly MethodInfo _waitToReadAsyncMethodInfo;

        public ReflectionChannelAsyncEnumerator(object channelReader, CancellationToken cancellationToken)
        {
            _channelReader = channelReader;
            _waitToReadArgs = [cancellationToken];

            var type = channelReader.GetType();
            _tryReadMethodInfo = (MethodInfo)type.GetMemberWithSameMetadataDefinitionAs(_channelReaderTryReadMethodInfo)!;
            _waitToReadAsyncMethodInfo = (MethodInfo)type.GetMemberWithSameMetadataDefinitionAs(_channelReaderWaitToReadAsyncMethodInfo)!;
        }

        public object? Current { get; private set; }

        public ValueTask<bool> MoveNextAsync()
        {
            if ((bool)_tryReadMethodInfo.Invoke(_channelReader, _tryReadResult)!)
            {
                Current = _tryReadResult[0];
                return new ValueTask<bool>(true);
            }

            return MoveNextAsyncAwaited();
        }

        private async ValueTask<bool> MoveNextAsyncAwaited()
        {
            while (await ((ValueTask<bool>)_waitToReadAsyncMethodInfo.Invoke(_channelReader, _waitToReadArgs)!).ConfigureAwait(false))
            {
                if ((bool)_tryReadMethodInfo.Invoke(_channelReader, _tryReadResult)!)
                {
                    Current = _tryReadResult[0];
                    return true;
                }
            }
            return false;
        }

        public ValueTask DisposeAsync() => default;
    }

#endif
}
