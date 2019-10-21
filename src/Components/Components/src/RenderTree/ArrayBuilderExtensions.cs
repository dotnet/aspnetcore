// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

#if IGNITOR
namespace Ignitor
#else
namespace Microsoft.AspNetCore.Components.RenderTree
#endif
{
    internal static class ArrayBuilderExtensions
    {
        /// <summary>
        /// Produces an <see cref="ArrayRange{T}"/> structure describing the current contents.
        /// </summary>
        /// <returns>The <see cref="ArrayRange{T}"/>.</returns>
        public static ArrayRange<T> ToRange<T>(this ArrayBuilder<T> builder)
            => new ArrayRange<T>(builder.Buffer, builder.Count);

        /// <summary>
        /// Produces an <see cref="ArrayBuilderSegment{T}"/> structure describing the selected contents.
        /// </summary>
        /// <param name="builder">The <see cref="ArrayBuilder{T}"/></param>
        /// <param name="fromIndexInclusive">The index of the first item in the segment.</param>
        /// <param name="toIndexExclusive">One plus the index of the last item in the segment.</param>
        /// <returns>The <see cref="ArraySegment{T}"/>.</returns>
        public static ArrayBuilderSegment<T> ToSegment<T>(this ArrayBuilder<T> builder, int fromIndexInclusive, int toIndexExclusive)
            => new ArrayBuilderSegment<T>(builder, fromIndexInclusive, toIndexExclusive - fromIndexInclusive);
    }
}
