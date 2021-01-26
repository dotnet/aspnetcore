// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A collection of media types.
    /// </summary>
    public class MediaTypeCollection : Collection<string>
    {
        /// <summary>
        /// Adds an object to the end of the <see cref="MediaTypeCollection"/>. 
        /// </summary>
        /// <param name="item">The media type to be added to the end of the <see cref="MediaTypeCollection"/>.</param>
        public void Add(MediaTypeHeaderValue item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            Add(item.ToString());
        }

        /// <summary>
        /// Inserts an element into the <see cref="MediaTypeCollection"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The media type to insert.</param>
        public void Insert(int index, MediaTypeHeaderValue item)
        {
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            Insert(index, item.ToString());
        }

        /// <summary>
        /// Removes the first occurrence of a specific media type from the <see cref="MediaTypeCollection"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns><see langword="true" /> if <paramref name="item"/> is successfully removed; otherwise, <see langword="false" />.
        /// This method also returns <see langword="false" /> if <paramref name="item"/> was not found in the original 
        /// <see cref="MediaTypeCollection"/>.</returns>
        public bool Remove(MediaTypeHeaderValue item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return Remove(item.ToString());
        }
    }
}
