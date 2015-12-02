// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Html;

namespace Microsoft.AspNet.Mvc.Razor.Buffer
{
    /// <summary>
    /// Encapsulates a string or <see cref="IHtmlContent"/> value.
    /// </summary>
    public struct RazorValue
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RazorValue"/> with a <c>string</c> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public RazorValue(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RazorValue"/> with a <see cref="IHtmlContent"/> value.
        /// </summary>
        /// <param name="value">The <see cref="IHtmlContent"/>.</param>
        public RazorValue(IHtmlContent content)
        {
            Value = content;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Writes the <see cref="Value"/> by encoding it with the specified <paramref name="encoder"/> to the
        /// specified <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write the value to.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/> which encodes the content to be written.</param>
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (Value == null)
            {
                return;
            }

            var stringValue = Value as string;
            if (stringValue != null)
            {
                writer.Write(stringValue);
            }
            else
            {
                Debug.Assert(Value is IHtmlContent);
                var htmlContentValue = (IHtmlContent)Value;
                htmlContentValue.WriteTo(writer, encoder);
            }
        }
    }
}
