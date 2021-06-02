// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Collections.Generic;

namespace System.Runtime.RateLimits
{
    public struct PermitLease : IDisposable
    {
        // This represents whether permit lease acquisition was successful
        public bool IsAcquired { get; }

        // This represents additional metadata that can be returned as part of a call to Acquire/AcquireAsync
        // Potential uses could include a RetryAfter value or an error code.
        public IReadOnlyDictionary<MetadataName, object>? Metadata { get; }

        private IDisposable? _disposable;

        // `state` is the additional metadata, onDispose takes `state` as its argument.
        public PermitLease(bool isAcquired, IReadOnlyDictionary<MetadataName, object?>? metadata, IDisposable? disposable)
        {
            IsAcquired = isAcquired;
            Metadata = metadata;
            _disposable = disposable;
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
