// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
/// of the Blazor framework. These types will change in future release.
/// </summary>
/// <typeparam name="T">The type of the elements in the array</typeparam>
//
// Represents a range of elements within an instance of <see cref="ArrayBuilder{T}"/>.
public readonly struct ArrayBuilderSegment<T> : IEnumerable<T>
{
    // The following fields are memory mapped to the WASM client. Do not re-order or use auto-properties.
    private readonly ArrayBuilder<T>? _builder;
    private readonly int _offset;
    private readonly int _count;

    internal ArrayBuilderSegment(ArrayBuilder<T> builder, int offset, int count)
    {
        _builder = builder;
        _offset = offset;
        _count = count;
    }

    /// <summary>
    /// Gets the current underlying array holding the segment's elements.
    /// </summary>
    public T[] Array => _builder?.Buffer ?? System.Array.Empty<T>();

    /// <summary>
    /// Gets the offset into the underlying array holding the segment's elements.
    /// </summary>
    public int Offset => _offset;

    /// <summary>
    /// Gets the number of items in the segment.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the specified item from the segment.
    /// </summary>
    /// <param name="index">The index into the segment.</param>
    /// <returns>The array entry at the specified index within the segment.</returns>
    public T this[int index]
        => Array[_offset + index];

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => ((IEnumerable<T>)new ArraySegment<T>(Array, _offset, _count)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)new ArraySegment<T>(Array, _offset, _count)).GetEnumerator();

    // TODO: If this assembly later moves to netstandard2.1, consider adding a public
    // GetEnumerator method that returns ArraySegment.Enumerator to avoid boxing.
}
