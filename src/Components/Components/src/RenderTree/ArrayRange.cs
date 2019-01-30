// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// Represents a range of elements in an array that are in use.
    /// </summary>
    /// <typeparam name="T">The array item type.</typeparam>
    public readonly struct ArrayRange<T> : IEnumerable, IEnumerable<T>
    {
        /// <summary>
        /// Gets the underlying array instance.
        /// </summary>
        public readonly T[] Array;

        /// <summary>
        /// Gets the number of items in the array that are considered to be in use.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Constructs an instance of <see cref="ArrayRange{T}"/>.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="count">The number of items in the array that are in use.</param>
        public ArrayRange(T[] array, int count)
        {
            Array = array;
            Count = count;
        }

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => ((IEnumerable<T>)new ArraySegment<T>(Array, 0, Count)).GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)new ArraySegment<T>(Array, 0, Count)).GetEnumerator();

        /// <summary>
        /// Creates a shallow clone of the instance.
        /// </summary>
        /// <returns></returns>
        public ArrayRange<T> Clone()
        {
            var buffer = new T[Count];
            System.Array.Copy(Array, buffer, Count);
            return new ArrayRange<T>(buffer, Count);
        }
    }
}
