// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class AntiforgerySerializationContextPooledObjectPolicy : IPooledObjectPolicy<AntiforgerySerializationContext>
{
    public AntiforgerySerializationContext Create()
    {
        return new AntiforgerySerializationContext();
    }

    public bool Return(AntiforgerySerializationContext obj)
    {
        obj.Reset();

        return true;
    }
}
