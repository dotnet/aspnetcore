// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// A <see cref="PooledObjectPolicy{T}"/> for <see cref="ResponseBufferingStream"/>.
/// </summary>
internal sealed class ResponseBufferingStreamPooledObjectPolicy : PooledObjectPolicy<ResponseBufferingStream>
{
    /// <inheritdoc />
    public override ResponseBufferingStream Create()
    {
        return new ResponseBufferingStream();
    }

    /// <inheritdoc />
    public override bool Return(ResponseBufferingStream obj)
    {
        obj.ResetForPool();
        return true;
    }
}
