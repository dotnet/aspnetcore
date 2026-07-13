// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.ResponseCaching;

internal sealed class CacheKeyDelimiterException : Exception
{
    public CacheKeyDelimiterException()
        : base("The value contains invalid characters to cache.")
    {
    }
}
