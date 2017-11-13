// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    // True-internal because this is a weird and tricky class to use :)
    internal static class AsyncEnumeratorAdapters
    {
        private static readonly MethodInfo _boxEnumeratorMethod = typeof(AsyncEnumeratorAdapters)
            .GetRuntimeMethods()
            .Single(m => m.Name.Equals(nameof(BoxEnumerator)) && m.IsGenericMethod);

        private static readonly MethodInfo _fromObservableMethod = typeof(AsyncEnumeratorAdapters)
            .GetRuntimeMethods()
            .Single(m => m.Name.Equals(nameof(FromObservable)) && m.IsGenericMethod);

        private static readonly MethodInfo _getAsyncEnumeratorMethod = typeof(AsyncEnumeratorAdapters)
            .GetRuntimeMethods()
            .Single(m => m.Name.Equals(nameof(GetAsyncEnumerator)) && m.IsGenericMethod);

        public static IAsyncEnumerator<object> FromObservable(object observable, Type observableInterface, CancellationToken cancellationToken)
        {
            // TODO: Cache expressions by observable.GetType()?
            return (IAsyncEnumerator<object>)_fromObservableMethod
                .MakeGenericMethod(observableInterface.GetGenericArguments())
                .Invoke(null, new[] { observable, cancellationToken });
        }

        public static IAsyncEnumerator<object> FromObservable<T>(IObservable<T> observable, CancellationToken cancellationToken)
        {
            // TODO: Allow bounding and optimizations?
            var channel = Channel.CreateUnbounded<object>();

            var subscription = observable.Subscribe(new ChannelObserver<T>(channel.Writer, cancellationToken));

            // Dispose the subscription when the token is cancelled
            cancellationToken.Register(state => ((IDisposable)state).Dispose(), subscription);

            return GetAsyncEnumerator(channel.Reader, cancellationToken);
        }

        public static IAsyncEnumerator<object> FromChannel(object readableChannelOfT, Type payloadType, CancellationToken cancellationToken)
        {
            var enumerator = _getAsyncEnumeratorMethod
                .MakeGenericMethod(payloadType)
                .Invoke(null, new object[] { readableChannelOfT, cancellationToken });

            if (payloadType.IsValueType)
            {
                return (IAsyncEnumerator<object>)_boxEnumeratorMethod
                    .MakeGenericMethod(payloadType)
                    .Invoke(null, new[] { enumerator });
            }
            else
            {
                return (IAsyncEnumerator<object>)enumerator;
            }
        }

        private static IAsyncEnumerator<object> BoxEnumerator<T>(IAsyncEnumerator<T> input) where T : struct
        {
            return new BoxingEnumerator<T>(input);
        }

        private class ChannelObserver<T> : IObserver<T>
        {
            private ChannelWriter<object> _output;
            private CancellationToken _cancellationToken;

            public ChannelObserver(ChannelWriter<object> output, CancellationToken cancellationToken)
            {
                _output = output;
                _cancellationToken = cancellationToken;
            }

            public void OnCompleted()
            {
                _output.TryComplete();
            }

            public void OnError(Exception error)
            {
                _output.TryComplete(error);
            }

            public void OnNext(T value)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    // Noop, someone else is handling the cancellation
                    return;
                }

                // This will block the thread emitting the object if the channel is bounded and full
                // I think this is OK, since we want to push the backpressure up. However, we may need
                // to find a way to force the entire subscription off to a dedicated thread in order to
                // ensure we don't block other tasks

                // Right now however, we use unbounded channels, so all of the above is moot because TryWrite will always succeed
                while (!_output.TryWrite(value))
                {
                    // Wait for a spot
                    if (!_output.WaitToWriteAsync(_cancellationToken).Result)
                    {
                        // Channel was closed.
                        throw new InvalidOperationException("Output channel was closed");
                    }
                }
            }
        }

        private class BoxingEnumerator<T> : IAsyncEnumerator<object> where T : struct
        {
            private IAsyncEnumerator<T> _input;

            public BoxingEnumerator(IAsyncEnumerator<T> input)
            {
                _input = input;
            }

            public object Current => _input.Current;
            public Task<bool> MoveNextAsync() => _input.MoveNextAsync();
        }

        public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(ChannelReader<T> channel, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new AsyncEnumerator<T>(channel, cancellationToken);
        }

        /// <summary>Provides an async enumerator for the data in a channel.</summary>
        internal class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            /// <summary>The channel being enumerated.</summary>
            private readonly ChannelReader<T> _channel;
            /// <summary>Cancellation token used to cancel the enumeration.</summary>
            private readonly CancellationToken _cancellationToken;
            /// <summary>The current element of the enumeration.</summary>
            private T _current;

            internal AsyncEnumerator(ChannelReader<T> channel, CancellationToken cancellationToken)
            {
                _channel = channel;
                _cancellationToken = cancellationToken;
            }

            public T Current => _current;

            public Task<bool> MoveNextAsync()
            {
                ValueTask<T> result = _channel.ReadAsync(_cancellationToken);

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
