// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.ResponseCaching;

/// Default implementation for <see cref="IResponseCachingFeature" />
public class ResponseCachingFeature : IResponseCachingFeature
{
    private string[]? _varyByQueryKeys;

    /// <inheritdoc />
    public string[]? VaryByQueryKeys
    {
        get
        {
            return _varyByQueryKeys;
        }
        set
        {
            if (value?.Length > 1)
            {
                for (var i = 0; i < value.Length; i++)
                {
                    if (string.IsNullOrEmpty(value[i]))
                    {
                        throw new ArgumentException($"When {nameof(value)} contains more than one value, it cannot contain a null or empty value.", nameof(value));
                    }
                }
            }
            _varyByQueryKeys = value;
        }
    }
}
