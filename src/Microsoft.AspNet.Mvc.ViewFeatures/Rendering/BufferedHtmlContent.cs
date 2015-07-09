// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Enumerable object collection which knows how to write itself.
    /// </summary>
    public class BufferedHtmlContent : IHtmlContent
    {
        private const int MaxCharToStringLength = 1024;
        // This is not List<IHtmlContent> because that would lead to boxing all strings to IHtmlContent
        // which is not space performant.
        // internal for testing.
        internal List<object> Entries { get; } = new List<object>();

        /// <summary>
        /// Appends the string to the collection.
        /// </summary>
        /// <param name="value">The <c>string</c> to be appended.</param>
        public void Append([NotNull] string value)
        {
            Entries.Add(value);
        }

        /// <summary>
        /// Appends a character array to the collection.
        /// </summary>
        /// <param name="value">The character array to be appended.</param>
        /// <param name="index">The index from which the character array must be read.</param>
        /// <param name="count">The count till which the character array must be read.</param>
        /// <remarks>
        /// Splits the character array into strings of 1KB length and appends them.
        /// </remarks>
        public void Append([NotNull] char[] value, int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (count < 0 || value.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (count > 0)
            {
                // Split large char arrays into 1KB strings.
                var currentCount = count;
                if (MaxCharToStringLength < currentCount)
                {
                    currentCount = MaxCharToStringLength;
                }

                Append(new string(value, index, currentCount));
                index += currentCount;
                count -= currentCount;
            }
        }

        /// <summary>
        /// Appends a <see cref="IHtmlContent"/> to the collection.
        /// </summary>
        /// <param name="htmlContent">The <see cref="IHtmlContent"/> to be appended.</param>
        public void Append([NotNull] IHtmlContent htmlContent)
        {
            Entries.Add(htmlContent);
        }

        /// <summary>
        /// Removes all the entries from the collection.
        /// </summary>
        public void Clear()
        {
            Entries.Clear();
        }

        /// <inheritdoc />
        public void WriteTo([NotNull] TextWriter writer, [NotNull] IHtmlEncoder encoder)
        {
            foreach (var entry in Entries)
            {
                var entryAsString = entry as string;
                if (entryAsString != null)
                {
                    writer.Write(entryAsString);
                }
                else
                {
                    // Only string, IHtmlContent values can be added to the buffer.
                    ((IHtmlContent)entry).WriteTo(writer, encoder);
                }
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer, new HtmlEncoder());
                return writer.ToString();
            }
        }
    }
}
