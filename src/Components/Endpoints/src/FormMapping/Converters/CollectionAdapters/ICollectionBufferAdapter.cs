// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal interface ICollectionBufferAdapter<TCollection, TBuffer, TElement>
{
    public static abstract TBuffer CreateBuffer();
    public static abstract TBuffer Add(ref TBuffer buffer, TElement element);
    public static abstract TCollection ToResult(TBuffer buffer);
}
