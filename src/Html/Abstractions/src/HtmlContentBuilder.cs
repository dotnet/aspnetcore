// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Html
{
    /// <summary>
    /// An <see cref="IHtmlContentBuilder"/> implementation using an in memory list.
    /// </summary>
    public class HtmlContentBuilder : IHtmlContentBuilder
    {
        /// <summary>
        /// Creates a new <see cref="HtmlContentBuilder"/>.
        /// </summary>
        public HtmlContentBuilder()
            : this(new List<object>())
        {
        }

        /// <summary>
        /// Creates a new <see cref="HtmlContentBuilder"/> with the given initial capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the backing store.</param>
        public HtmlContentBuilder(int capacity)
            : this(new List<object>(capacity))
        {
        }

        /// <summary>
        /// Gets the number of elements in the <see cref="HtmlContentBuilder"/>.
        /// </summary>
        public int Count => Entries.Count;

        /// <summary>
        /// Creates a new <see cref="HtmlContentBuilder"/> with the given list of entries.
        /// </summary>
        /// <param name="entries">
        /// The list of entries. The <see cref="HtmlContentBuilder"/> will use this list without making a copy.
        /// </param>
        public HtmlContentBuilder(IList<object> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            Entries = entries;
        }

        // This is not List<IHtmlContent> because that would lead to wrapping all strings to IHtmlContent
        // which is not space performant.
        //
        // In general unencoded strings are added here. We're optimizing for that case, and allocating
        // a wrapper when encoded strings are used.
        //
        // internal for testing.
        internal IList<object> Entries { get; }

        /// <inheritdoc />
        public IHtmlContentBuilder Append(string unencoded)
        {
            if (!string.IsNullOrEmpty(unencoded))
            {
                Entries.Add(unencoded);
            }

            return this;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder AppendHtml(IHtmlContent htmlContent)
        {
            if (htmlContent == null)
            {
                return this;
            }

            Entries.Add(htmlContent);
            return this;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder AppendHtml(string encoded)
        {
            if (!string.IsNullOrEmpty(encoded))
            {
                Entries.Add(new HtmlString(encoded));
            }

            return this;
        }

        /// <inheritdoc />
        public IHtmlContentBuilder Clear()
        {
            Entries.Clear();
            return this;
        }

        /// <inheritdoc />
        public void CopyTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                string entryAsString;
                IHtmlContentContainer entryAsContainer;
                if ((entryAsString = entry as string)  != null)
                {
                    destination.Append(entryAsString);
                }
                else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
                {
                    // Since we're copying, do a deep flatten.
                    entryAsContainer.CopyTo(destination);
                }
                else
                {
                    // Only string, IHtmlContent values can be added to the buffer.
                    destination.AppendHtml((IHtmlContent)entry);
                }
            }
        }

        /// <inheritdoc />
        public void MoveTo(IHtmlContentBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                string entryAsString;
                IHtmlContentContainer entryAsContainer;
                if ((entryAsString = entry as string) != null)
                {
                    destination.Append(entryAsString);
                }
                else if ((entryAsContainer = entry as IHtmlContentContainer) != null)
                {
                    // Since we're moving, do a deep flatten.
                    entryAsContainer.MoveTo(destination);
                }
                else
                {
                    // Only string, IHtmlContent values can be added to the buffer.
                    destination.AppendHtml((IHtmlContent)entry);
                }
            }

            Entries.Clear();
        }

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                var entryAsString = entry as string;
                if (entryAsString != null)
                {
                    encoder.Encode(writer, entryAsString);
                }
                else
                {
                    // Only string, IHtmlContent values can be added to the buffer.
                    ((IHtmlContent)entry).WriteTo(writer, encoder);
                }
            }
        }

        private string DebuggerToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }
    }
}
