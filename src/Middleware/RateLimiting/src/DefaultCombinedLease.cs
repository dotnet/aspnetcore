// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;

internal sealed class DefaultCombinedLease : RateLimitLease
{
    private readonly RateLimitLease _globalLease;
    private readonly RateLimitLease _endpointLease;
    private HashSet<string>? _metadataNames;

    public DefaultCombinedLease(RateLimitLease globalLease, RateLimitLease endpointLease)
    {
        _globalLease = globalLease;
        _endpointLease = endpointLease;
    }

    public override bool IsAcquired => true;

    public override IEnumerable<string> MetadataNames
    {
        get
        {
            if (_metadataNames is null)
            {
                _metadataNames = new HashSet<string>();
                foreach (var metadataName in _globalLease.MetadataNames)
                {
                    _metadataNames.Add(metadataName);
                }
                foreach (var metadataName in _endpointLease.MetadataNames)
                {
                    _metadataNames.Add(metadataName);
                }
            }
            return _metadataNames;
        }
    }

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        // Use the first metadata item of a given name, ignore duplicates, we can't reliably merge arbitrary metadata
        // Creating an object[] if there are multiple of the same metadataName could work, but makes consumption of metadata messy
        // and makes MetadataName.Create<T>(...) uses no longer work
        if (_endpointLease.TryGetMetadata(metadataName, out metadata))
        {
            return true;
        }
        if (_globalLease.TryGetMetadata(metadataName, out metadata))
        {
            return true;
        }

        metadata = null;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        List<Exception>? exceptions = null;

        // Dispose endpoint lease first, then global lease (reverse order of when they were acquired)
        // Avoids issues where dispose might unblock a queued acquire and then the acquire fails when acquiring the next limiter.
        // When disposing in reverse order there wont be any issues of unblocking an acquire that affects acquires on limiters in the chain after it
        try
        {
            _endpointLease.Dispose();
        }
        catch (Exception ex)
        {
            exceptions ??= new List<Exception>();
            exceptions.Add(ex);
        }

        try
        {
            _globalLease.Dispose();
        }
        catch (Exception ex)
        {
            exceptions ??= new List<Exception>(1);
            exceptions.Add(ex);
        }

        if (exceptions is not null)
        {
            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }
            else
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
