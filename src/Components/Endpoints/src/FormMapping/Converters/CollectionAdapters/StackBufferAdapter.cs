// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class StackBufferAdapter<TElement> : ICollectionBufferAdapter<Stack<TElement>, Stack<TElement>, TElement>
{
    public static Stack<TElement> CreateBuffer() => new();

    public static Stack<TElement> Add(ref Stack<TElement> buffer, TElement element)
    {
        buffer.Push(element);
        return buffer;
    }

    public static Stack<TElement> ToResult(Stack<TElement> buffer) => buffer;
}
