// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class ResettableCompletionSource : IValueTaskSource<uint>
    {
        private ManualResetValueTaskSourceCore<uint> _valueTaskSource;
        private readonly MsQuicStream _stream;

        public ResettableCompletionSource(MsQuicStream stream)
        {
            _stream = stream;
            _valueTaskSource.RunContinuationsAsynchronously = true;
        }

        public ValueTask<uint> GetValueTask()
        {
            return new ValueTask<uint>(this, _valueTaskSource.Version);
        }

        public uint GetResult(short token)
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
                    _valueTaskSource.Reset();
                    _stream._resettableCompletion = this;
                }
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _valueTaskSource.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _valueTaskSource.OnCompleted(continuation, state, token, flags);
        }

        public void Complete(uint result)
        {
            _valueTaskSource.SetResult(result);
        }
    }
}
