// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.Framework.Internal
{
    /// <summary>
    /// Enumerable object collection which knows how to write itself.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    internal class BufferedHtmlContent : IHtmlContentBuilder
    {
        // This is not List<IHtmlContent> because that would lead to wrapping all strings to IHtmlContent
        // which is not space performant.
        // internal for testing.
        internal List<object> Entries { get; } = new List<object>();

        /// <summary>
        /// Appends the <see cref="string"/> to the collection.
        /// </summary>
        /// <param name="value">The <c>string</c> to be appended.</param>
        /// <returns>A reference to this instance after the Append operation has completed.</returns>
        public IHtmlContentBuilder Append(string value)
        {
            Entries.Add(value);
            return this;
        }

        /// <summary>
        /// Appends a <see cref="IHtmlContent"/> to the collection.
        /// </summary>
        /// <param name="htmlContent">The <see cref="IHtmlContent"/> to be appended.</param>
        /// <returns>A reference to this instance after the Append operation has completed.</returns>
        public IHtmlContentBuilder Append(IHtmlContent htmlContent)
        {
            Entries.Add(htmlContent);
            return this;
        }

        /// <summary>
        /// Appends the HTML encoded <see cref="string"/> to the collection.
        /// </summary>
        /// <param name="value">The HTML encoded <c>string</c> to be appended.</param>
        /// <returns>A reference to this instance after the Append operation has completed.</returns>
        public IHtmlContentBuilder AppendEncoded(string value)
        {
            Entries.Add(new HtmlEncodedString(value));
            return this;
        }
        /// <summary>
        /// Removes all the entries from the collection.
        /// </summary>
        /// <returns>A reference to this instance after the Clear operation has completed.</returns>
        public IHtmlContentBuilder Clear()
        {
            Entries.Clear();
            return this;
        }

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            foreach (var entry in Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                var entryAsString = entry as string;
                if (entryAsString != null)
                {
                    encoder.HtmlEncode(entryAsString, writer);
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

        private class HtmlEncodedString : IHtmlContent
        {
            public static readonly IHtmlContent NewLine = new HtmlEncodedString(Environment.NewLine);

            private readonly string _value;

            public HtmlEncodedString(string value)
            {
                _value = value;
            }

            public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
            {
                writer.Write(_value);
            }
        }
    }
}
