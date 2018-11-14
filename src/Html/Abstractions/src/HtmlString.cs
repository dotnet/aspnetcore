// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Html
{
    /// <summary>
    /// An <see cref="IHtmlContent"/> implementation that wraps an HTML encoded <see cref="string"/>.
    /// </summary>
    public class HtmlString : IHtmlContent
    {
        /// <summary>
        /// An <see cref="HtmlString"/> instance for <see cref="Environment.NewLine"/>.
        /// </summary>
        public static readonly HtmlString NewLine = new HtmlString(Environment.NewLine);

        /// <summary>
        /// An <see cref="HtmlString"/> instance for <see cref="string.Empty"/>.
        /// </summary>
        public static readonly HtmlString Empty = new HtmlString(string.Empty);

        /// <summary>
        /// Creates a new <see cref="HtmlString"/>.
        /// </summary>
        /// <param name="value">The HTML encoded value.</param>
        public HtmlString(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the HTML encoded value.
        /// </summary>
        public string Value { get; }

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

            writer.Write(Value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}
