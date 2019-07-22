using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    internal class ResettableBooleanCompletionSource : IValueTaskSource<bool>
    {
        ManualResetValueTaskSourceCore<bool> _mrvts;
        private readonly LIFOQueuePolicy _queue;

        public ResettableBooleanCompletionSource(LIFOQueuePolicy queue)
        {
            _queue = queue;
        }

        public ValueTask<bool> Task()
        {
            return new ValueTask<bool>(this, _mrvts.Version);
        }

        bool IValueTaskSource<bool>.GetResult(short token)
        {
            var isValid = token == _mrvts.Version;
            try
            {
                return _mrvts.GetResult(token);
            }
            finally
            {
                if (isValid)
                {
                    _mrvts.Reset();
                    _queue._cachedResettableTCS = this;
                }
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _mrvts.GetStatus(token);
        }

        void IValueTaskSource<bool>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _mrvts.OnCompleted(continuation, state, token, flags);
        }

        public void Complete(bool result)
        {
            _mrvts.SetResult(result);
        }
    }
}
