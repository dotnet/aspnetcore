// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class AsyncEnumerableReader : IValueTaskSource<int>
    {
        private readonly Action _onCompletedAction;

        private ManualResetValueTaskSourceCore<int> _valueTaskSource;
        private IAsyncEnumerable<int> _readerSource;
        private IAsyncEnumerator<int> _reader;

        public Memory<byte> Buffer { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        private ValueTaskAwaiter<bool> _readAwaiter;

        private volatile bool _inProgress;
        public bool InProgress => _inProgress;

        public AsyncEnumerableReader()
        {
            _onCompletedAction = OnCompleted;
        }

        internal void Initialize(IAsyncEnumerable<int> readerSource)
        {
            _readerSource = readerSource;
            _reader = readerSource.GetAsyncEnumerator();
        }

        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_readerSource is null)
            {
                ThrowNotInitialized();
            }

            if (_inProgress)
            {
                ThrowConcurrentReadsNotSupported();
            }
            _inProgress = true;

            Buffer = buffer;
            CancellationToken = cancellationToken;

            var task = _reader.MoveNextAsync();
            _readAwaiter = task.GetAwaiter();

            return new ValueTask<int>(this, _valueTaskSource.Version);
        }

        int IValueTaskSource<int>.GetResult(short token)
        {
            var isValid = token == _valueTaskSource.Version;
            try
            {
                return _valueTaskSource.GetResult(token);
            }
            finally
            {
                if (isValid)
                {
                    Buffer = default;
                    CancellationToken = default;
                    _inProgress = false;
                    _valueTaskSource.Reset();
                }
            }
        }

        ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token)
            => _valueTaskSource.GetStatus(token);

        void IValueTaskSource<int>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (!InProgress)
            {
                ThrowNoReadInProgress();
            }

            _valueTaskSource.OnCompleted(continuation, state, token, flags);

            _readAwaiter.UnsafeOnCompleted(_onCompletedAction);
        }

        private void OnCompleted()
        {
            try
            {
                if (_readAwaiter.GetResult())
                {
                    _valueTaskSource.SetResult(_reader.Current);
                }
                else
                {
                    _valueTaskSource.SetResult(-1);
                }
            }
            catch (Exception ex)
            {
                // If the GetResult throws for this ReadAsync (e.g. cancellation),
                // that will cause all next ReadAsyncs to also throw, so we create
                // a fresh unerrored AsyncEnumerable to restore the next ReadAsyncs
                // to the normal flow
                _reader = _readerSource.GetAsyncEnumerator();
                _valueTaskSource.SetException(ex);
            }
        }

        static void ThrowConcurrentReadsNotSupported()
        {
            throw new InvalidOperationException("Concurrent reads are not supported");
        }

        static void ThrowNoReadInProgress()
        {
            throw new InvalidOperationException("No read in progress, await will not complete");
        }

        static void ThrowNotInitialized()
        {
            throw new InvalidOperationException(nameof(AsyncEnumerableReader) + " has not been initialized");
        }
    }
}
