using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.RateLimits
{
    public abstract class PermitLease : IDisposable
    {
        // This represents whether permit lease acquisition was successful
        public abstract bool IsAcquired { get; }

        // Extension methods to convert value of well known metadata to specific types.
        public abstract bool TryGetMetadata(MetadataName metadataName, [NotNullWhen(true)] out object? metadata);

        public abstract void Dispose();
    }
}
