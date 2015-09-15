// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Html.Abstractions
{
    /// <summary>
    /// Extension methods for <see cref="IHtmlContentBuilder"/>.
    /// </summary>
    public static class HtmlContentBuilderExtensions
    {
        /// <summary>
        /// Appends an <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder)
        {
            builder.Append(HtmlEncodedString.NewLine);
            return builder;
        }

        /// <summary>
        /// Appends an <see cref="Environment.NewLine"/> after appending the <see cref="string"/> value.
        /// The value is treated as unencoded as-provided, and will be HTML encoded before writing to output.
        /// </summary>
        /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
        /// <param name="unencoded">The <see cref="string"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder, string unencoded)
        {
            builder.Append(unencoded);
            builder.Append(HtmlEncodedString.NewLine);
            return builder;
        }

        /// <summary>
        /// Appends an <see cref="Environment.NewLine"/> after appending the <see cref="IHtmlContent"/> value.
        /// </summary>
        /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
        /// <param name="content">The <see cref="IHtmlContent"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder, IHtmlContent htmlContent)
        {
            builder.Append(htmlContent);
            builder.Append(HtmlEncodedString.NewLine);
            return builder;
        }

        /// <summary>
        /// Appends an <see cref="Environment.NewLine"/> after appending the <see cref="string"/> value.
        /// The value is treated as HTML encoded as-provided, and no further encoding will be performed.
        /// </summary>
        /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
        /// <param name="content">The HTML encoded <see cref="string"/> to append.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        public static IHtmlContentBuilder AppendLineEncoded(this IHtmlContentBuilder builder, string encoded)
        {
            builder.AppendEncoded(encoded);
            builder.Append(HtmlEncodedString.NewLine);
            return builder;
        }

        /// <summary>
        /// Sets the content to the <see cref="string"/> value. The value is treated as unencoded as-provided,
        /// and will be HTML encoded before writing to output.
        /// </summary>
        /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
        /// <param name="value">The <see cref="string"/> value that replaces the content.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        public static IHtmlContentBuilder SetContent(this IHtmlContentBuilder builder, string unencoded)
        {
            builder.Clear();
            builder.Append(unencoded);
            return builder;
        }

        /// <summary>
        /// Sets the content to the <see cref="IHtmlContent"/> value.
        /// </summary>
        /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
        /// <param name="value">The <see cref="IHtmlContent"/> value that replaces the content.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        public static IHtmlContentBuilder SetContent(this IHtmlContentBuilder builder, IHtmlContent content)
        {
            builder.Clear();
            builder.Append(content);
            return builder;
        }

        /// <summary>
        /// Sets the content to the <see cref="string"/> value. The value is treated as HTML encoded as-provided, and
        /// no further encoding will be performed.
        /// </summary>
        /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
        /// <param name="content">The HTML encoded <see cref="string"/> that replaces the content.</param>
        /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
        public static IHtmlContentBuilder SetContentEncoded(this IHtmlContentBuilder builder, string encoded)
        {
            builder.Clear();
            builder.AppendEncoded(encoded);
            return builder;
        }

        [DebuggerDisplay("{DebuggerToString()}")]
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
}
