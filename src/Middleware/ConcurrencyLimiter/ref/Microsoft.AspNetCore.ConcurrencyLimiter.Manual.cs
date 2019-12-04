// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    internal sealed partial class ConcurrencyLimiterEventSource : System.Diagnostics.Tracing.EventSource
    {
        public static readonly Microsoft.AspNetCore.ConcurrencyLimiter.ConcurrencyLimiterEventSource Log;
        internal ConcurrencyLimiterEventSource() { }
        internal ConcurrencyLimiterEventSource(string eventSourceName) { }
        protected override void OnEventCommand(System.Diagnostics.Tracing.EventCommandEventArgs command) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void QueueSkipped() { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public Microsoft.AspNetCore.ConcurrencyLimiter.ConcurrencyLimiterEventSource.QueueFrame QueueTimer() { throw null; }
        [System.Diagnostics.Tracing.EventAttribute(1, Level=System.Diagnostics.Tracing.EventLevel.Warning)]
        public void RequestRejected() { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal partial struct QueueFrame : System.IDisposable
        {
            private Microsoft.Extensions.Internal.ValueStopwatch? _timer;
            private Microsoft.AspNetCore.ConcurrencyLimiter.ConcurrencyLimiterEventSource _parent;
            public QueueFrame(Microsoft.Extensions.Internal.ValueStopwatch? timer, Microsoft.AspNetCore.ConcurrencyLimiter.ConcurrencyLimiterEventSource parent) { throw null; }
            public void Dispose() { }
        }
    }
    internal partial class QueuePolicy : Microsoft.AspNetCore.ConcurrencyLimiter.IQueuePolicy, System.IDisposable
    {
        public QueuePolicy(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ConcurrencyLimiter.QueuePolicyOptions> options) { }
        public int TotalRequests { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Dispose() { }
        public void OnExit() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<bool> TryEnterAsync() { throw null; }
    }
    internal partial class ResettableBooleanCompletionSource : System.Threading.Tasks.Sources.IValueTaskSource<bool>
    {
        public ResettableBooleanCompletionSource(Microsoft.AspNetCore.ConcurrencyLimiter.StackPolicy queue) { }
        public void Complete(bool result) { }
        public System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token) { throw null; }
        public System.Threading.Tasks.ValueTask<bool> GetValueTask() { throw null; }
        bool System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetResult(short token) { throw null; }
        void System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.OnCompleted(System.Action<object> continuation, object state, short token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags) { }
    }
    internal partial class StackPolicy : Microsoft.AspNetCore.ConcurrencyLimiter.IQueuePolicy
    {
        public Microsoft.AspNetCore.ConcurrencyLimiter.ResettableBooleanCompletionSource _cachedResettableTCS;
        public StackPolicy(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ConcurrencyLimiter.QueuePolicyOptions> options) { }
        public void OnExit() { }
        public System.Threading.Tasks.ValueTask<bool> TryEnterAsync() { throw null; }
    }
}
