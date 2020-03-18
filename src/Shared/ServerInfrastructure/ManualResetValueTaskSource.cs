// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal sealed class ManualResetValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core; // mutable struct; do not make this readonly

        public bool RunContinuationsAsynchronously { get => _core.RunContinuationsAsynchronously; set => _core.RunContinuationsAsynchronously = value; }
        public short Version => _core.Version;
        public void Reset() => _core.Reset();
        public void SetResult(T result) => _core.SetResult(result);
        public void SetException(Exception error) => _core.SetException(error);

        public T GetResult(short token) => _core.GetResult(token);
        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);

        public ValueTaskSourceStatus GetStatus() => _core.GetStatus(_core.Version);

        public void TrySetResult(T result)
        {
            if (_core.GetStatus(_core.Version) == ValueTaskSourceStatus.Pending)
            {
                _core.SetResult(result);
            }
        }
    }
}
