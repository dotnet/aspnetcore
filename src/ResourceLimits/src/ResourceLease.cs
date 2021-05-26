// Will be migrated to dotnet/runtime
// Pending dotnet API review

namespace System.Threading.ResourceLimits
{
    public struct ResourceLease : IDisposable
    {
        // This represents whether resource lease acquisition was successful
        public bool IsAcquired { get; }

        // This represents additional metadata that can be returned as part of a call to Acquire/AcquireAsync
        // Potential uses could include a RetryAfter value or an error code.
        public object? State { get; }

        private Action<object?>? _onDispose;

        // `state` is the additional metadata, onDispose takes `state` as its argument.
        public ResourceLease(bool isAcquired, object? state, Action<object?>? onDispose)
        {
            IsAcquired = isAcquired;
            State = state;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke(State);
        }

        // This static `ResourceLease` is used by rate limiters or in cases where `Acquire` returns false.
        public static ResourceLease SuccessfulAcquisition = new ResourceLease(true, null, null);
        public static ResourceLease FailedAcquisition = new ResourceLease(false, null, null);
    }
}
